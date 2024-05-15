using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.Common;
using IM.Commons;
using IM.Options;
using IM.Transfer.Dtos;
using Microsoft.AspNetCore.Http;
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
    private readonly IChannelContactAppService _channelContactAppAppService;

    public TransferAppService(
        IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<CAServerOptions> caServerOptions,
        IHttpContextAccessor httpContextAccessor,
        IChannelContactAppService channelContactAppAppService)
    {
        _httpClientProvider = httpClientProvider;
        _caServerOptions = caServerOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _channelContactAppAppService = channelContactAppAppService;
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
        var result = await _channelContactAppAppService.GetChannelDetailInfoAsync(new ChannelDetailInfoRequestDto()
        {
            ChannelUuid = channelUuid
        });

        if (result == null || result.Uuid != channelUuid)
        {
            throw new UserFriendlyException("channel not exists, channelUuid:{channelUuid}", channelUuid);
        }

        var members = result.Members?.Select(t => t.UserId).ToList();

        if (members.IsNullOrEmpty())
        {
            throw new UserFriendlyException(
                "channel members is empty, channelUuid:{channelUuid}", channelUuid);
        }

        if (!members.Contains(CurrentUser.GetId()))
        {
            throw new UserFriendlyException(
                "sender is not in channel, channelUuid:{channelUuid}, userId{userId}",
                channelUuid, CurrentUser.GetId().ToString());
        }

        if (!members.Contains(Guid.Parse(toUserId)))
        {
            throw new UserFriendlyException(
                "to user is not in channel, channelUuid:{channelUuid}, toUserId:{toUserId}", channelUuid,
                toUserId);
        }
    }
}