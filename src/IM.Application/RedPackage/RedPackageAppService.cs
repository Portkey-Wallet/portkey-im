using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.Common;
using IM.Commons;
using IM.Grains.Grain.RedPackage;
using IM.Options;
using IM.RedPackage.Dtos;
using IM.RelationOne;
using IM.User.Provider;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace IM.RedPackage;

[DisableAuditing, RemoteService(false)]
public class RedPackageAppService : ImAppService, IRedPackageAppService
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly CAServerOptions _caServerOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserProvider _userProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IChannelContactAppService _channelContactAppAppService;

    public RedPackageAppService(
        IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<CAServerOptions> caServerOptions,
        IHttpContextAccessor httpContextAccessor,
        IChannelContactAppService channelContactAppAppService,
        IUserProvider userProvider,
        IClusterClient clusterClient)
    {
        _httpClientProvider = httpClientProvider;
        _caServerOptions = caServerOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _userProvider = userProvider;
        _clusterClient = clusterClient;
        _channelContactAppAppService = channelContactAppAppService;
    }

    public async Task<GenerateRedPackageOutputDto> GenerateRedPackageAsync(GenerateRedPackageInputDto redPackageInput)
    {
        var headers = BuildReqHeader();
        var result =
            await _httpClientProvider.PostAsync<GenerateRedPackageOutputDto>(
                _caServerOptions.BaseUrl + CAServerConstant.GenerateRedPackage, redPackageInput, headers);
        return result;
    }

    public async Task<SendRedPackageOutputDto> SendRedPackageAsync(SendRedPackageInputDto input)
    {
        if (!await CheckChannelAsync(input.ChannelUuid))
        {
            throw new UserFriendlyException("channel do not exist");
        }

        var headers = BuildReqHeader();
        var result =
            await _httpClientProvider.PostAsync<SendRedPackageOutputDto>(
                _caServerOptions.BaseUrl + CAServerConstant.SendRedPackage, input, headers);
        return result;
    }

    public async Task<GetCreationResultOutputDto> GetCreationResultAsync(Guid sessionId)
    {
        var headers = BuildReqHeader();
        var result =
            await _httpClientProvider.GetAsync<GetCreationResultOutputDto>(
                _caServerOptions.BaseUrl + CAServerConstant.GetCreationResult + $"?sessionId={sessionId}", headers);
        return result;
    }

    public async Task<RedPackageDetailDto> GetRedPackageDetailAsync(Guid id, int skipCount = 0, int maxResultCount = 0)
    {
        var headers = BuildReqHeader();
        var result =
            await _httpClientProvider.GetAsync<RedPackageDetailDto>(
                _caServerOptions.BaseUrl + CAServerConstant.GetRedPackageDetail +
                $"?id={id}&skipCount={skipCount}&maxResultCount={maxResultCount}", headers);
        //update user ViewStatus
        var grain = _clusterClient.GetGrain<IRedPackageUserGrain>(
            RedPackageHelper.BuildUserViewKey(CurrentUser.GetId(), id));

        await grain.SetUserViewStatus(GetUserViewStatus(result.Status, result.IsCurrentUserGrabbed, true));
        result.ViewStatus = (await grain.GetUserViewStatus()).Data;
        return result;
    }

    public async Task<RedPackageConfigOutput> GetRedPackageConfigAsync([CanBeNull] string chainId,
        [CanBeNull] string token)
    {
        var headers = BuildReqHeader();
        string url = _caServerOptions.BaseUrl + CAServerConstant.GetRedPackageConfig;

        if (!string.IsNullOrEmpty(token))
        {
            url += $"?token={token}";
        }

        if (!string.IsNullOrEmpty(chainId))
        {
            url += string.IsNullOrEmpty(token) ? $"?chainId={chainId}" : $"&chainId={chainId}";
        }

        var result = await _httpClientProvider.GetAsync<RedPackageConfigOutput>(url);
        return result;
    }

    public async Task<GrabRedPackageOutputDto> GrabRedPackageAsync(GrabRedPackageInputDto input)
    {
        if (input.Id == Guid.Empty)
        {
            throw new UserFriendlyException("RedPackage Id is empty");
        }

        var grain = _clusterClient.GetGrain<IRedPackageUserGrain>(
            RedPackageHelper.BuildUserViewKey(CurrentUser.GetId(), input.Id));

        var headers = BuildReqHeader();

        var getUserTask = _userProvider.GetUserInfoByIdAsync(CurrentUser.GetId());
        var getDetailTask = _httpClientProvider.GetAsync<RedPackageDetailDto>(
            _caServerOptions.BaseUrl + CAServerConstant.GetRedPackageDetail +
            $"?id={input.Id}&skipCount={0}&maxResultCount={0}", headers);
        var getChannelTask = _channelContactAppAppService.GetChannelDetailInfoAsync(new ChannelDetailInfoRequestDto()
        {
            ChannelUuid = input.ChannelUuid
        });

        await Task.WhenAll(getUserTask, getDetailTask, getChannelTask);

        var user = await getUserTask;
        var detail = await getDetailTask;
        var channelDetail = await getChannelTask;

        if (user == null)
        {
            throw new UserFriendlyException("user not exist");
        }

        var address = user.CaAddresses.Where(x => x.ChainId == detail.ChainId).Select(x => x.Address)
            .FirstOrDefault();

        if (address.IsNullOrEmpty())
        {
            var holderInfo = await _userProvider.GetCaHolderInfoAsync(user.CaHash);
            address = holderInfo?.CaHolderInfo?.Where(t => t.ChainId == detail.ChainId).Select(f => f.CaAddress)
                .FirstOrDefault();
        }

        input.UserCaAddress = address;
        if (string.IsNullOrEmpty(input.UserCaAddress))
        {
            throw new UserFriendlyException("user do not have ca address in chain " + detail.ChainId);
        }

        if (channelDetail == null)
        {
            throw new UserFriendlyException("Channel not exist");
        }

        if (!string.Equals(channelDetail.Uuid, detail.ChannelUuid))
        {
            throw new UserFriendlyException("invalid channel uuid");
            ;
        }

        if (!CheckPermissions(channelDetail, detail))
        {
            throw new UserFriendlyException("User lacks permission to grab the red packet");
        }

        var result =
            await _httpClientProvider.PostAsync<GrabRedPackageOutputDto>(
                _caServerOptions.BaseUrl + CAServerConstant.GrabRedPackage, input, headers);
        await grain.SetUserViewStatus(GetUserViewStatus(result.Status, true,
            result.Result.Equals(RedPackageGrabStatus.Success)));
        result.ViewStatus = (await grain.GetUserViewStatus()).Data;
        return result;
    }

    private bool CheckPermissions(ChannelDetailInfoResponseDto channelDetail, RedPackageDetailDto redPackageDetail)
    {
        if (channelDetail.Members.Any(x => x.UserId == CurrentUser.GetId()))
        {
            return true;
        }

        return false;
    }

    private async Task<bool> CheckChannelAsync(string channelUuid)
    {
        try
        {
            var result = await _channelContactAppAppService.GetChannelDetailInfoAsync(new ChannelDetailInfoRequestDto()
            {
                ChannelUuid = channelUuid
            });
            if (result.Uuid == channelUuid)
            {
                return true;
            }

            return false;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "check channel error, channelUuid:{channelUuid}", channelUuid);
            return false;
        }
    }

    private Dictionary<string, string> BuildReqHeader()
    {
        var authToken = _httpContextAccessor.HttpContext?.Request.Headers[CommonConstant.AuthHeader];
        var relationAuthToken = _httpContextAccessor.HttpContext?.Request.Headers[RelationOneConstant.AuthHeader];

        var headers = new Dictionary<string, string>
        {
            { CommonConstant.AuthHeader, authToken },
            { RelationOneConstant.AuthHeader, relationAuthToken }
        };
        return headers;
    }


    private UserViewStatus GetUserViewStatus(RedPackageStatus input, bool isCurrentUserGrabbed, bool grabStatus)
    {
        if (isCurrentUserGrabbed && grabStatus)
        {
            return UserViewStatus.Opened;
        }

        switch (input)
        {
            case RedPackageStatus.Cancelled:
                return UserViewStatus.Expired;
            case RedPackageStatus.Expired:
                return UserViewStatus.Expired;
            case RedPackageStatus.FullyClaimed:
                return UserViewStatus.NoneLeft;
        }

        return UserViewStatus.Init;
    }
}