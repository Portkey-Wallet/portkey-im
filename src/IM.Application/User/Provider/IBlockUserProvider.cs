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
        parameters.Add("@uId", blockUserInfoDto.UId);
        parameters.Add("@blockUId", blockUserInfoDto.BlockUId);
        // parameters.Add("@CreateTime", blockUserInfoDto.CreateTime);
        // parameters.Add("@UpdateTime", blockUserInfoDto.UpdateTime);
        var sql =
            "INSERT INTO block_user_info (uid, block_uid) VALUES (@uId, @blockUId);";
        await _imRepository.ExecuteAsync(sql, parameters);
    }

    public async Task<BlockUserInfoDto> GetBlockUserInfoAsync(string id, string reportUserId)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@uId", id);
        parameters.Add("@blockUserId", reportUserId);
        var sql =
            "select id,uid AS uId,block_uid AS blockUid from block_user_info where uid = @uId and block_uid = @blockUserId";
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
            "select id,uid AS uId,block_uid AS blockUid from block_user_info where uid = @uId and is_effective = 1";
        var userInfoDtos = await _imRepository.QueryAsync<BlockUserInfoDto>(sql, parameters);
        return userInfoDtos?.ToList();
    }
}