using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using IM.Common;
using IM.Entities.Es;
using IM.User.Dtos;
using Nest;
using Volo.Abp.DependencyInjection;

namespace IM.User.Provider;

public interface IUserProvider
{
    Task<CaHolderInfoDto> GetCaHolderInfoAsync(string caHash);
    Task<UserIndex> GetUserInfoAsync(string relationId);
    Task<UserIndex> GetUserInfoAsync(Guid userId, string caAddress);
    Task<List<UserIndex>> ListUserInfoAsync(List<Guid> userIds, string caAddress);
    Task<UserIndex> GetUserInfoByIdAsync(Guid userId);
    Task UpdateUserInfoAsync(Guid userId, string walletName, string avatar);
}

public class UserProvider : IUserProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INESTRepository<UserIndex, Guid> _userRepository;

    public UserProvider(INESTRepository<UserIndex, Guid> userRepository, IGraphQLHelper graphQlHelper)
    {
        _userRepository = userRepository;
        _graphQlHelper = graphQlHelper;
    }

    public async Task<CaHolderInfoDto> GetCaHolderInfoAsync(string caHash)
    {
        return await _graphQlHelper.QueryAsync<CaHolderInfoDto>(new GraphQLRequest
        {
            Query = @"
        			    query($caHash:String,$skipCount:Int!,$maxResultCount:Int!) {
                            caHolderInfo(dto: {caHash:$caHash,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                                    id,chainId,caHash,caAddress,originChainId,managerInfos{address,extraData},guardianList{guardians{verifierId,identifierHash,salt,isLoginGuardian,type}}}
                        }",
            Variables = new
            {
                caHash, skipCount = 0, maxResultCount = 10
            }
        });
    }

    public async Task<UserIndex> GetUserInfoAsync(string relationId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserIndex>, QueryContainer>>()
        {
            descriptor => descriptor.Term(i => i.Field(f => f.RelationId).Value(relationId))
        };

        QueryContainer Filter(QueryContainerDescriptor<UserIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _userRepository.GetAsync(Filter);
    }

    public async Task<UserIndex> GetUserInfoAsync(Guid userId, string caAddress)
    {
        if (userId == Guid.Empty && caAddress.IsNullOrWhiteSpace()) return null;
        var mustQuery = new List<Func<QueryContainerDescriptor<UserIndex>, QueryContainer>>() { };

        if (userId != Guid.Empty)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(userId)));
        }

        mustQuery.Add(q => q.Terms(t => t.Field("caAddresses.address").Terms(caAddress)));
        QueryContainer Filter(QueryContainerDescriptor<UserIndex> f) => f.Bool(b => b.Must(mustQuery));
        var res = await _userRepository.GetListAsync(Filter);

        return res?.Item2?.FirstOrDefault();
    }

    public async Task<List<UserIndex>> ListUserInfoAsync(List<Guid> userIds, string caAddress)
    {
        if ((userIds == null || !userIds.Any()) && caAddress.IsNullOrWhiteSpace())
        {
            return new List<UserIndex>();
        }

        var shouldQuery = new List<Func<QueryContainerDescriptor<UserIndex>, QueryContainer>>();

        if (userIds != null && userIds.Any())
        {
            shouldQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(userIds)));
        }

        if (!caAddress.IsNullOrWhiteSpace())
        {
            shouldQuery.Add(q => q.Terms(t => t.Field("caAddresses.address").Terms(caAddress)));
        }

        QueryContainer Filter(QueryContainerDescriptor<UserIndex> f) => f.Bool(b => b.Should(shouldQuery));
        var res = await _userRepository.GetListAsync(Filter, limit: 50);

        return res?.Item2;
    }

    public async Task<UserIndex> GetUserInfoByIdAsync(Guid userId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserIndex>, QueryContainer>>()
        {
            descriptor => descriptor.Term(i => i.Field(f => f.Id).Value(userId))
        };

        QueryContainer Filter(QueryContainerDescriptor<UserIndex> f) => f.Bool(b => b.Must(mustQuery));
        return await _userRepository.GetAsync(Filter);
    }

    public async Task UpdateUserInfoAsync(Guid userId, string walletName, string avatar)
    {
        var user = await GetUserInfoByIdAsync(userId);
        if (user == null) return;

        if (!walletName.IsNullOrWhiteSpace())
        {
            user.Name = walletName;
        }

        if (!avatar.IsNullOrWhiteSpace())
        {
            user.Avatar = avatar;
        }

        await _userRepository.UpdateAsync(user);
    }
}