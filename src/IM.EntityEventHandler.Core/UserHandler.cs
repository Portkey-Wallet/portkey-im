using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using IM.Commons;
using IM.Entities.Es;
using IM.Grains.Grain;
using IM.Options;
using IM.User.Dtos;
using IM.User.Etos;
using IM.User.Provider;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using IObjectMapper = Volo.Abp.ObjectMapping.IObjectMapper;

namespace IM.EntityEventHandler.Core;

public class UserHandler : IDistributedEventHandler<AddUserEto>, ITransientDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<UserIndex, Guid> _userRepository;
    private readonly ILogger<UserHandler> _logger;
    private readonly UserAddressOptions _userAddressOptions;
    private readonly IUserProvider _userProvider;
    private readonly IClusterClient _clusterClient;

    public UserHandler(IObjectMapper objectMapper,
        INESTRepository<UserIndex, Guid> userRepository,
        ILogger<UserHandler> logger,
        IOptionsSnapshot<UserAddressOptions> userAddressOptions, IUserProvider userProvider,
        IClusterClient clusterClient)
    {
        _objectMapper = objectMapper;
        _userRepository = userRepository;
        _logger = logger;
        _userProvider = userProvider;
        _clusterClient = clusterClient;
        _userAddressOptions = userAddressOptions.Value;
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

            _ = AddAddressAsync(user);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Add or update user error, userId:{userId}, caHash:{caHash}, relationId:{relationId}",
                eventData.Id.ToString(), eventData.CaHash, eventData.RelationId);
        }
    }

    private async Task AddAddressAsync(UserIndex user)
    {
        try
        {
            if (user.CaAddresses.Count > CommonConstant.RegisterChainCount)
            {
                return;
            }

            var retryCount = 1;
            while (_userAddressOptions.RetryCount > retryCount)
            {
                await Task.Delay(TimeSpan.FromSeconds(_userAddressOptions.WaitSeconds));
                var holderInfo = await _userProvider.GetCaHolderInfoAsync(user.CaHash);

                if (holderInfo.CaHolderInfo is { Count: CommonConstant.RegisterChainCount })
                {
                    retryCount++;
                    continue;
                }

                var guardianDto = holderInfo.CaHolderInfo.First(t => t.CaAddress != user.CaAddresses.First().Address);
                var addResult = await AddAddressAsync(guardianDto, user);
                if (addResult)
                {
                    break;
                }
                
                retryCount++;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "add address error, userId:{userId}, caHash:{caHash}, relationId:{relationId}",
                user.Id, user.CaHash, user.RelationId);
        }
    }

    private async Task<bool> AddAddressAsync(GuardianDto guardianDto, UserIndex user)
    {
        try
        {
            var userGrain = _clusterClient.GetGrain<IUserGrain>(user.Id);
            var resultDto = await userGrain.AddAddress(new CaAddressInfo()
            {
                ChainId = guardianDto.ChainId,
                Address = guardianDto.CaAddress,
                ChainName = CommonConstant.DefaultChainName
            });

            if (!resultDto.Success())
            {
                _logger.LogWarning(
                    "add address fail, code:{code}, userId:{userId}, chainId:{chainId}, address:{address}",
                    resultDto.Code, user.Id, guardianDto.ChainId, guardianDto.CaAddress);
                return false;
            }

            user.CaAddresses = resultDto.Data.CaAddresses;
            await _userRepository.AddOrUpdateAsync(user);

            _logger.LogInformation(
                "add address success, userId:{userId}, chainId:{chainId}, address:{address}",
                user.Id, guardianDto.ChainId, guardianDto.CaAddress);

            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "add address error, userId:{userId}, chainId:{chainId}, address:{address}",
                user.Id, guardianDto.ChainId, guardianDto.CaAddress);
            return false;
        }
    }
}