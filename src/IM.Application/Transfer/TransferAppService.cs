using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IM.ChannelContact;
using IM.ChannelContactService.Provider;
using IM.Common;
using IM.Commons;
using IM.Options;
using IM.RedPackage.Provider;
using IM.Transfer.Dtos;
using IM.User.Provider;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace IM.Transfer;

[RemoteService(false), DisableAuditing]
public class TransferAppService : ImAppService, ITransferAppService
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly CAServerOptions _caServerOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IRedPackageProvider _packageProvider;
    private readonly IChannelProvider _channelProvider;
    private readonly IUserProvider _userProvider;

    public TransferAppService(
        IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<CAServerOptions> caServerOptions,
        IHttpContextAccessor httpContextAccessor,
        IRedPackageProvider packageProvider,
        IChannelProvider channelProvider,
        IUserProvider userProvider)
    {
        _httpClientProvider = httpClientProvider;
        _caServerOptions = caServerOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _packageProvider = packageProvider;
        _channelProvider = channelProvider;
        _userProvider = userProvider;
    }

    public async Task<TransferOutputDto> SendTransferAsync(TransferInputDto input)
    {
        await CheckChannelAsync(input.ChannelUuid, input.ToUserId);
        var headers = BuildReqHeader();

        var result =
            await _httpClientProvider.PostAsync<TransferOutputDto>(
                _caServerOptions.BaseUrl + CAServerConstant.SendTransfer, input, headers);
        return result;
    }

    public async Task<TransferResultDto> GetResultAsync(string transferId)
    {
        var headers = BuildReqHeader();
        var result =
            await _httpClientProvider.GetAsync<TransferResultDto>(
                $"{_caServerOptions.BaseUrl}{CAServerConstant.GetTransferResult}?transferId={transferId}", headers);
        return result;
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

    private async Task CheckChannelAsync(string channelUuid, string toUserId)
    {
        if (!await CheckChannelAsync(channelUuid))
        {
            throw new UserFriendlyException("channel not exists, channelUuid:{channelUuid}", channelUuid);
        }

        var currentUser = await _userProvider.GetUserInfoByIdAsync(CurrentUser.GetId());
        if (!await CheckUserAsync(channelUuid, currentUser.RelationId))
        {
            throw new UserFriendlyException(
                "sender is not in channel, channelUuid:{channelUuid}, userId{userId}",
                channelUuid, CurrentUser.GetId().ToString());
        }

        var toUser = await _userProvider.GetUserInfoByIdAsync(Guid.Parse(toUserId));

        if (!await CheckUserAsync(channelUuid, toUser.RelationId))
        {
            throw new UserFriendlyException(
                "to user is not in channel, channelUuid:{channelUuid}, toUserId:{toUserId}", channelUuid,
                toUserId);
        }
    }

    private async Task<bool> CheckUserAsync(string channelUuid, string relationId)
    {
        var memberInfo = await _packageProvider.GetMemberAsync(channelUuid, relationId);
        return memberInfo != null;
    }

    private async Task<bool> CheckChannelAsync(string channelUuid)
    {
        try
        {
            var result = await _channelProvider.GetChannelInfoByUUIDAsync(channelUuid);
            return result != null && result.Uuid == channelUuid;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "check channel error, channelUuid:{channelUuid}", channelUuid);
            return false;
        }
    }
}