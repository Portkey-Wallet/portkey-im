using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using IM.Entities.Es;
using IM.User.Etos;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;

namespace IM.EntityEventHandler.Core;

public class UserHandler : IDistributedEventHandler<AddUserEto>, ITransientDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<UserIndex, Guid> _userRepository;
    private readonly ILogger<UserHandler> _logger;

    public UserHandler(IObjectMapper objectMapper,
        INESTRepository<UserIndex, Guid> userRepository, ILogger<UserHandler> logger)
    {
        _objectMapper = objectMapper;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(AddUserEto eventData)
    {
        try
        {
            var user = _objectMapper.Map<AddUserEto, UserIndex>(eventData);
            await _userRepository.AddOrUpdateAsync(user);

            _logger.LogInformation(
                "Add or update user success, userId:{userId}, caHash:{caHash}, relationId:{relationId}",
                eventData.Id.ToString(), eventData.CaHash, eventData.RelationId);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Add or update user error, userId:{userId}, caHash:{caHash}, relationId:{relationId}",
                eventData.Id.ToString(), eventData.CaHash, eventData.RelationId);
        }
    }
}