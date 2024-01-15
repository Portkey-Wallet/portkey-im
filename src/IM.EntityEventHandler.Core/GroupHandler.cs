using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using IM.ChannelContact.Etos;
using IM.Entities.Es;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace IM.EntityEventHandler.Core;

public class GroupHandler : IDistributedEventHandler<GroupAddOrUpdateEto>, IDistributedEventHandler<GroupDeleteEto>,
    ITransientDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<GroupIndex, string> _groupRepository;
    private readonly ILogger<UserHandler> _logger;

    public GroupHandler(IObjectMapper objectMapper,
        INESTRepository<GroupIndex, string> groupRepository, ILogger<UserHandler> logger)
    {
        _objectMapper = objectMapper;
        _groupRepository = groupRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(GroupAddOrUpdateEto eventData)
    {
        try
        {
            var group = _objectMapper.Map<GroupAddOrUpdateEto, GroupIndex>(eventData);
            await _groupRepository.AddOrUpdateAsync(group);

            _logger.LogInformation(
                "Add or update group success, groupId: {groupId}", eventData.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Add or update group error, userId: {groupId}", eventData.Id);
        }
    }

    public async Task HandleEventAsync(GroupDeleteEto eventData)
    {
        try
        {
            await _groupRepository.DeleteAsync(eventData.Id);

            _logger.LogInformation(
                "Delete group success, groupId: {groupId}", eventData.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Delete group error, groupId: {groupId}", eventData.Id);
        }
    }
}