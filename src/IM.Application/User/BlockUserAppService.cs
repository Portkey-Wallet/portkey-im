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
    private readonly IUserProvider _userProvider;

    public BlockUserAppService(IBlockUserProvider blockUserProvider, IUserProvider userProvider)
    {
        _blockUserProvider = blockUserProvider;
        _userProvider = userProvider;
    }

    public async Task<string> BlockUserAsync(BlockUserRequestDto input)
    {
        if (CurrentUser.Id == null)
        {
            throw new UserFriendlyException("UnLogin,please try again");
        }

        var userIndex = await _userProvider.GetUserInfoByIdAsync((Guid)CurrentUser.Id);
        var blockUser = new BlockUserInfoDto
        {
            RelationId = userIndex.RelationId,
            BlockRelationId = input.RelationId
        };

        var blockUserInfo = await _blockUserProvider.GetBlockUserInfoAsync(userIndex.RelationId, input.RelationId);

        switch (blockUserInfo)
        {
            case { IsEffective: 0 }:
                throw new UserFriendlyException("You have already blocked.");
            case { IsEffective: 1 }:
                await _blockUserProvider.ReBlockUserInfoAsync(blockUserInfo.Id);
                return "success";
            default:
                await _blockUserProvider.BlockUserAsync(blockUser);
                return "success";
        }
    }

    public async Task<string> UnBlockUserAsync(UnBlockUserRequestDto input)
    {
        if (CurrentUser.Id == null)
        {
            throw new UserFriendlyException("UnLogin,please try again");
        }

        var userIndex = await _userProvider.GetUserInfoByIdAsync((Guid)CurrentUser.Id);
        var blockUserInfo = await _blockUserProvider.GetBlockUserInfoAsync(userIndex.RelationId, input.RelationId);
        if (null == blockUserInfo)
        {
            throw new UserFriendlyException("User have not been blocked.");
        }
        await _blockUserProvider.UnBlockUserInfoAsync(blockUserInfo.Id);
        return "success";
    }

    public async Task<bool> IsBlockedAsync(BlockUserRequestDto input)
    {
        if (CurrentUser.Id == null)
        {
            throw new UserFriendlyException("UnLogin,please try again");
        }

        var userIndex = await _userProvider.GetUserInfoByIdAsync((Guid)CurrentUser.Id);
        var blockUserInfo = await _blockUserProvider.GetBlockUserInfoAsync(userIndex.RelationId, input.RelationId);
        if (null != blockUserInfo)
        {
            return blockUserInfo.IsEffective == 0;
        }

        return false;
    }

    public async Task<List<string>> BlockListAsync()
    {
        if (CurrentUser.Id == null)
        {
            throw new UserFriendlyException("UnLogin,please try again");
        }
        var userIndex = await _userProvider.GetUserInfoByIdAsync((Guid)CurrentUser.Id);
        var list = await _blockUserProvider.GetBlockUserListAsync(userIndex.RelationId);
        return list.Select(t => t.BlockRelationId).ToList();
    }
}