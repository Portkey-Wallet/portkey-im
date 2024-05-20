using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IM.User.Dtos;
using IM.User.Provider;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace IM.User;

[RemoteService(false), DisableAuditing]
public class BlockUserAppService : ImAppService, IBlockUserAppService
{
    private readonly IBlockUserProvider _blockUserProvider;

    public BlockUserAppService(IBlockUserProvider blockUserProvider)
    {
        _blockUserProvider = blockUserProvider;
    }

    public async Task<string> BlockUserAsync(BlockUserRequestDto input)
    {
        var id = CurrentUser.Id.ToString();
        var blockUser = new BlockUserInfoDto
        {
            UId = id,
            BlockUId = input.UserId,
            CreateTime = new DateTime(),
            UpdateTime = new DateTime()
        };
        await _blockUserProvider.BlockUserAsync(blockUser);
        return "success";
    }

    public async Task<string> UnBlockUserAsync(UnBlockUserRequestDto input)
    {
        var id = CurrentUser.Id.ToString();
        var blockUserInfo = await _blockUserProvider.GetBlockUserInfoAsync(id, input.UserId);
        if (null != blockUserInfo)
        {
            await _blockUserProvider.UnBlockUserInfoAsync(blockUserInfo.Id);
        }

        return "success";
    }

    public async Task<bool> IsBlockedAsync(BlockUserRequestDto input)
    {
        var id = CurrentUser.Id.ToString();
        var blockUserInfo = await _blockUserProvider.GetBlockUserInfoAsync(id, input.UserId);
        if (null != blockUserInfo)
        {
            return blockUserInfo.IsEffective == 0;
        }

        return false;
    }

    public async Task<List<string>> BlockListAsync()
    {
        var id = CurrentUser.Id.ToString();
        var list = await _blockUserProvider.GetBlockUserListAsync(id);
        return list.Select(t => t.BlockUId).ToList();
    }
}