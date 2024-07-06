using System.Threading.Tasks;
using IM.ChatBot;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace IM.EntityEventHandler.Core.Worker;

public class InitChatBotUsageRankWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IChatBotAppService _chatBotAppService;
    
    public InitChatBotUsageRankWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, IChatBotAppService chatBotAppService) : base(timer, serviceScopeFactory)
    {
        _chatBotAppService = chatBotAppService;
        Timer.Period = 3000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _chatBotAppService.InitBotUsageRankAsync();
    }
}