using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using IM.Dapper.Repository;
using IM.User.Dtos;
using Volo.Abp.DependencyInjection;

namespace IM.User.Provider;

public interface IBlockUserProvider
{
    Task BlockUserAsync(BlockUserInfoDto blockUserInfoDto);

    Task<BlockUserInfoDto> GetBlockUserInfoAsync(string id, string inputUserId);
    Task<string> UnBlockUserInfoAsync(int id);
    Task<List<BlockUserInfoDto>> GetBlockUserListAsync(string id);
    Task ReBlockUserInfoAsync(int id);
}

public class BlockUserProvider : IBlockUserProvider, ISingletonDependency
{
    private readonly IImRepository _imRepository;

    public BlockUserProvider(IImRepository imRepository)
    {
        _imRepository = imRepository;
    }

    public async Task BlockUserAsync(BlockUserInfoDto blockUserInfoDto)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@relationId", blockUserInfoDto.RelationId);
        parameters.Add("@blockRelationId", blockUserInfoDto.BlockRelationId);
  
        var sql =
            "INSERT INTO block_user_info (relation_id, block_relation_id) VALUES (@relationId, @blockRelationId);";
        await _imRepository.ExecuteAsync(sql, parameters);
    }

    public async Task<BlockUserInfoDto> GetBlockUserInfoAsync(string id, string reportUserId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@relationId", id);
        parameters.Add("@blockRelationId", reportUserId);
        var sql =
            "select id,relation_id AS RelationId,block_relation_id AS blockRelationId, is_effective AS isEffective from block_user_info where relation_id = @relationId and block_relation_id = @blockRelationId";
        var userInfoDto = await _imRepository.QueryFirstOrDefaultAsync<BlockUserInfoDto>(sql, parameters);
        return userInfoDto;
    }

    public async Task<string> UnBlockUserInfoAsync(int id)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id);
        var sql = "update block_user_info set is_effective = 1 where id = @Id";
        await _imRepository.ExecuteAsync(sql, parameters);
        return "success";
    }

    public async Task<List<BlockUserInfoDto>> GetBlockUserListAsync(string id)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@uId", id);
        var sql =
            "select id,relation_id AS relationId,block_relation_id AS blockRelationId from block_user_info where relation_id = @uId and is_effective = 0";
        var userInfoDtos = await _imRepository.QueryAsync<BlockUserInfoDto>(sql, parameters);
        return userInfoDtos?.ToList();
    }

    public async Task ReBlockUserInfoAsync(int id)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@Id", id);
        var sql = "update block_user_info set is_effective = 0 where id = @Id";
        await _imRepository.ExecuteAsync(sql, parameters);
    }
}