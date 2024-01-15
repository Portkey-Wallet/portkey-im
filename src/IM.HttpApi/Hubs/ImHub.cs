using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.EventBus.Distributed;

namespace IM.Hubs;

public class ImHub : AbpHub
{
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IHubService _hubService;
    private readonly ILogger<ImHub> _logger;

    public ImHub(ILogger<ImHub> logger, IHubService hubService, IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _hubService = hubService;
        _distributedEventBus = distributedEventBus;
    }


    public async Task Connect(string clientId)
    {
        if (string.IsNullOrEmpty(clientId))
        {
            return;
        }

        await _hubService.RegisterClient(clientId, Context.ConnectionId);
        _logger.LogInformation("clientId={ClientId} connect", clientId);
        await _hubService.SendAllUnreadRes(clientId);
    }

    public async Task Ack(string clientId, string requestId)
    {
        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(requestId))
        {
            return;
        }

        await _hubService.Ack(clientId, requestId);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        var clientId = _hubService.UnRegisterClient(Context.ConnectionId);
        _logger.LogInformation("clientId={ClientId} disconnected!!!", clientId);
        return base.OnDisconnectedAsync(exception);
    }
}