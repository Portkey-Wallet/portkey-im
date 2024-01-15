using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using IM.Entities.Es;
using IM.Feed.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace IM.EntityEventHandler.Core;

public class MuteHandler : IDistributedEventHandler<MuteEto>, ITransientDependency
{
    private readonly ILogger<MessageHandler> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<MuteIndex, string> _muteRepository;

    public MuteHandler(IObjectMapper objectMapper, ILogger<MessageHandler> logger,
        INESTRepository<MuteIndex, string> muteRepository)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _muteRepository = muteRepository;
    }

    public async Task HandleEventAsync(MuteEto eventData)
    {
        try
        {
            var user = _objectMapper.Map<MuteEto, MuteIndex>(eventData);
            await _muteRepository.AddOrUpdateAsync(user);

            _logger.LogInformation(
                "modify mute success, userId:{userId}, groupId:{caHash}, mute:{mute}",
                eventData.UserId.ToString(), eventData.GroupId, eventData.Mute);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "modify mute error, userId:{userId}, groupId:{caHash}, mute:{mute}",
                eventData.UserId.ToString(), eventData.GroupId, eventData.Mute);
        }
    }
}