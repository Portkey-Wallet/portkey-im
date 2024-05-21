using System.Collections.Generic;
using System.Threading.Tasks;
using IM.User.Dtos;

namespace IM.User;

public interface IBlockUserAppService
{
    Task<string> BlockUserAsync(BlockUserRequestDto input);
    Task<string> UnBlockUserAsync(UnBlockUserRequestDto input);
    Task<bool> IsBlockedAsync(BlockUserRequestDto input);
    Task<List<string>> BlockListAsync();
}