using IM.ChannelContact.Dto;
using Orleans;

namespace IM.Grains.Grain.Group;

public interface IGroupGrain : IGrainWithStringKey
{
    Task<GrainResultDto<GroupGrainDto>> AddGroup(GroupGrainDto group);
    Task<GrainResultDto<GroupGrainDto>> UpdateGroup(GroupGrainDto group);
    Task<GrainResultDto<GroupGrainDto>> DeleteGroup();
    Task<GrainResultDto<List<GroupMember>>> GetMembers();
    Task<bool> Exist();
    Task<GrainResultDto<GroupGrainDto>> LeaveGroup(string userId);
}