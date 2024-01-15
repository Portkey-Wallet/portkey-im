using System.Collections.Generic;
using System.Threading.Tasks;
using IM.User.Dtos;

namespace IM.User;

public interface IBaseUserAppService
{
    Task<SignatureDto> GetSignatureAsync(SignatureRequestDto input);
    Task<SignatureDto> GetAuthTokenAsync(AuthRequestDto input);
    Task<UserInfoDto> GetUserInfoAsync(UserInfoRequestDto input);
    Task<List<AddressInfoDto>> GetAddressesAsync(string relationId);
    Task UpdateImUserAsync(ImUsrUpdateDto input);
    Task<List<UserInfoListDto>> ListUserInfoAsync(UserInfoListRequestDto input);
}