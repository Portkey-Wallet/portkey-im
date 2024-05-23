using System;
using System.Threading.Tasks;
using IM.ChannelContact.Dto;
using IM.Chat;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Users;

namespace IM.Message.Provider;

public interface IUnreadMessageUpdateProvider
{
    Task UpdateUnReadMessageCountAsync(string channelUuid, string authToken);
}

public class UnreadMessageUpdateProvider : IUnreadMessageUpdateProvider, ISingletonDependency
{
    private readonly IProxyMessageAppService _proxyMessageAppService;
    private readonly IProxyUnreadMessageService _proxyUnreadMessageService;
    private readonly ILogger<UnreadMessageUpdateProvider> _logger;
    private readonly ICurrentUser _currentUser;

    public UnreadMessageUpdateProvider(IProxyMessageAppService proxyMessageAppService,
        IProxyUnreadMessageService proxyUnreadMessageService,
        ILogger<UnreadMessageUpdateProvider> logger, ICurrentUser currentUser)
    {
        _proxyMessageAppService = proxyMessageAppService;
        _proxyUnreadMessageService = proxyUnreadMessageService;
        _logger = logger;
        _currentUser = currentUser;
    }

    public async Task UpdateUnReadMessageCountAsync(string channelUuid, string authToken)
    {
        try
        {
            if (channelUuid.IsNullOrWhiteSpace())
            {
                _logger.LogWarning("update unread message count fail, channelUuid is empty, groupId:{groupId}",
                    channelUuid);
                return;
            }

            if (authToken.IsNullOrWhiteSpace())
            {
                _logger.LogWarning("update unread message count fail, auth token is empty, groupId:{groupId}",
                    channelUuid);
                return;
            }

            var unreadCountResponseDto = await _proxyMessageAppService.GetUnreadMessageCountWithTokenAsync(authToken);

            var unreadMessageDto = new UnreadMessageDto
            {
                UserId = _currentUser.GetId().ToString(),
                DeviceId = string.Empty,
                NetworkType = NetworkType.MainNet,
                UnreadCount = unreadCountResponseDto.UnreadCount
            };

            await _proxyUnreadMessageService.UpdateUnReadMessageCountAsync(unreadMessageDto);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "update unread message count fail, channel uuid:{0}", channelUuid);
        }
    }
}