using System.Threading.Tasks;
using Dapper;
using IM.Dapper.Repository;
using IM.User.Dtos;

namespace IM.User.Provider;

public interface IBlockUserProvider
{
    Task BlockUserAsync(BlockUserInfoDto blockUserInfoDto);

    Task<BlockUserInfoDto> GetBlockUserInfoAsync(string id, string inputUserId);
    Task<string> UnBlockUserInfoAsync(int id);
}

public class BlockUserProvider : IBlockUserProvider
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
        parameters.Add("@CreateTime", blockUserInfoDto.CreateTime);
        parameters.Add("@UpdateTime", blockUserInfoDto.UpdateTime);
        var sql =
            "INSERT INTO block_user_info (uid, block_user_id,create_time,update_time) VALUES (@uId, @blockUId,@CreateTime,@UpdateTime);";
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
        var sql = "delete from block_user_info where id = @Id";
        await _imRepository.ExecuteAsync(sql, parameters);
        return "success";
    }
}