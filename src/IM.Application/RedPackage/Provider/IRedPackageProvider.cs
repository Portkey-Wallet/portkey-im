using System.Threading.Tasks;
using Dapper;
using IM.ChannelContact.Dto;
using IM.Dapper.Repository;
using Volo.Abp.DependencyInjection;

namespace IM.RedPackage.Provider;

public interface IRedPackageProvider
{
    Task<MemberInfo> GetMemberAsync(string channelUuid, string relationId);
}

public class RedPackageProvider : IRedPackageProvider, ISingletonDependency
{
    private readonly IImRepository _imRepository;

    public RedPackageProvider(IImRepository imRepository)
    {
        _imRepository = imRepository;
    }

    public async Task<MemberInfo> GetMemberAsync(string channelUuid, string relationId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@relationId", relationId);
        parameters.Add("@channelUuid", channelUuid);

        var sql =
            "select relation_id as RelationId from im_channel_member where channel_uuid=@channelUuid and status=0 and relation_id=@relationId;";
        var imUserInfo = await _imRepository.QueryFirstOrDefaultAsync<MemberInfo>(sql, parameters);
        return imUserInfo;
    }
}