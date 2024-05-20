using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Dapper;
using GraphQL;
using IM.Common;
using IM.Dapper.Repository;
using IM.Entities.Es;
using IM.Message;
using IM.Message.Dtos;
using IM.User.Dtos;
using Nest;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace IM.User.Provider;

public interface IUserProvider
{
    Task<CaHolderInfoDto> GetCaHolderInfoAsync(string caHash);
    Task<UserIndex> GetUserInfoAsync(string relationId);
    Task<List<UserIndex>> GetUserInfosByRelationIdsAsync(List<string> relationIds);
    Task<UserIndex> GetUserInfoAsync(Guid userId, string caAddress);
    Task<List<UserIndex>> ListUserInfoAsync(List<Guid> userIds, string caAddress);
    Task<UserIndex> GetUserInfoByIdAsync(Guid userId);
    Task UpdateUserInfoAsync(Guid userId, string walletName, string avatar);
    Task ReportUser(ImUser user, ImUser reportedUser, ReportedMessage reportedMessage);

    Task<bool> IsMessageInChannelAsync(string channelUuid, string messageId);
}

public class UserProvider : IUserProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INESTRepository<UserIndex, Guid> _userRepository;
    private readonly IImRepository _imRepository;

    public UserProvider(INESTRepository<UserIndex, Guid> userRepository, IGraphQLHelper graphQlHelper, IImRepository imRepository)
    {
        _userRepository = userRepository;
        _graphQlHelper = graphQlHelper;
        _imRepository = imRepository;
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

    public async Task<List<UserIndex>> GetUserInfosByRelationIdsAsync(List<string> relationIds)
    {
        if (relationIds.IsNullOrEmpty()) return new List<UserIndex>();
        var mustQuery = new List<Func<QueryContainerDescriptor<UserIndex>, QueryContainer>>()
        {
            descriptor => descriptor.Terms(i => i.Field(f => f.RelationId).Terms(relationIds))
        };
        QueryContainer Filter(QueryContainerDescriptor<UserIndex> f) => f.Bool(b => b.Must(mustQuery));
        var (totalCount, data) = await _userRepository.GetListAsync(Filter);
        return data;
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
    
    public async Task ReportUser(ImUser user, ImUser reportedUser, ReportedMessage reportedMessage)
    {
        long currentTime = (DateTime.Now.ToUniversalTime().Ticks - 621355968000000000) / 10000;
        var parameters = new DynamicParameters();
        parameters.Add("@uid", user.PortkeyId);
        parameters.Add("@userAddressInfo", JsonConvert.SerializeObject(user.CaAddresses));
        parameters.Add("@reportedUserId", reportedUser.PortkeyId);
        parameters.Add("@reportedUserAddressInfo", JsonConvert.SerializeObject(reportedUser.CaAddresses));
        parameters.Add("@messageId", reportedMessage.MessageId);
        parameters.Add("@reportedType", reportedMessage.ReportType);
        parameters.Add("@reportedMessage", reportedMessage.Message);
        parameters.Add("@description", reportedMessage.Description);
        parameters.Add("@reportedTime", currentTime);
        parameters.Add("@createTime", currentTime);
        parameters.Add("@updateTime", currentTime);
        parameters.Add("@relationId", reportedMessage.ReportedRelationId);
        parameters.Add("@channelUuid", reportedMessage.ChannelUuid);
        var sql = "insert into report_message_info (uid, user_address_info, reported_user_id, reported_user_address_info, " +
                  "message_id, reported_type, reported_message, description, relation_id, channel_uuid, reported_time, create_time, update_time)" +
                  " values (@uid, @userAddressInfo, @reportedUserId, @reportedUserAddressInfo, @messageId, @reportedType, " +
                  "@reportedMessage, @description, @relationId, @channelUuid, @reportedTime, @createTime, @updateTime)";
        await _imRepository.ExecuteAsync(sql, parameters);
    }
    
    public async Task<bool> IsMessageInChannelAsync(string channelUuid, string messageId)
    {
        var messageInfo = await GetMessageByIdAsync(channelUuid,messageId);
        return messageInfo is { Status: 0 };
    }

    private async Task<IMMessageInfoDto> GetMessageByIdAsync(string channelUuid, string messageId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@channelUuid", channelUuid);
        parameters.Add("@messageId", messageId);

        var sql =
            "select id as Id,send_uuid as SendUuid,channel_uuid as ChannelUuid,quote_id as QuoteId , status as status, mentioned_user as mentionedUser from im_message where channel_uuid=@channelUuid  and id=@messageId limit 1;";
        var imMessageInfoDto = await _imRepository.QueryFirstOrDefaultAsync<IMMessageInfoDto>(sql, parameters);
        return imMessageInfoDto;
    }
}