using IM.Entities.Es;
using IM.Grains.Grain.User;
using Orleans;

namespace IM.Grains.Grain;

public interface IUserGrain : IGrainWithGuidKey
{
    Task<GrainResultDto<UserGrainDto>> AddUser(UserGrainDto user);
    Task<GrainResultDto<UserGrainDto>> UpdateUser(UserGrainDto user);
    Task<GrainResultDto<UserGrainDto>> DeleteUser();
    Task<bool> Exist();
    Task<bool> NeedUpdate();
    Task<GrainResultDto<UserGrainDto>> AddAddress(CaAddressInfo caAddressInfo);
}