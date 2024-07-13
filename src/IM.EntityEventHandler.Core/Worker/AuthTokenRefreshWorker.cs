using System.Threading.Tasks;
using IM.ChatBot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace IM.EntityEventHandler.Core.Worker;

public class AuthTokenRefreshWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IChatBotAppService _chatBotAppService;
    private readonly ILogger<AuthTokenRefreshWorker> _logger;

    public AuthTokenRefreshWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IChatBotAppService chatBotAppService, ILogger<AuthTokenRefreshWorker> logger) : base(timer, serviceScopeFactory)
    {
        _chatBotAppService = chatBotAppService;
        _logger = logger;
        Timer.Period = 3000;
    }


    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        _logger.LogDebug("Init chatBot data starting....");
        await _chatBotAppService.RefreshBotTokenAsync();
    }
}