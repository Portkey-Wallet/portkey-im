using IM.ChannelContact.Dto;
using IM.Commons;
using IM.Grains.State.Group;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace IM.Grains.Grain.Group;

public class GroupGrain : Grain<GroupState>, IGroupGrain
{
    private readonly IObjectMapper _objectMapper;

    public GroupGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<GroupGrainDto>> AddGroup(GroupGrainDto group)
    {
        var result = new GrainResultDto<GroupGrainDto>();
        if (State.IsDelete)
        {
            result.Code = CommonResult.GroupAlreadyExistCode;
            return result;
        }

        State = _objectMapper.Map<GroupGrainDto, GroupState>(group);
        State.Id = this.GetPrimaryKeyString();

        await WriteStateAsync();
        result.Data = _objectMapper.Map<GroupState, GroupGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<GroupGrainDto>> UpdateGroup(GroupGrainDto group)
    {
        var result = new GrainResultDto<GroupGrainDto>();
        if (State.Id.IsNullOrEmpty())
        {
            return await AddGroup(group);
        }

        if (State.IsDelete)
        {
            result.Code = CommonResult.GroupDeletedCode;
            return result;
        }

        State = _objectMapper.Map<GroupGrainDto, GroupState>(group);
        await WriteStateAsync();

        result.Data = _objectMapper.Map<GroupState, GroupGrainDto>(State);
        return result;
    }

    public async Task<GrainResultDto<GroupGrainDto>> DeleteGroup()
    {
        var result = new GrainResultDto<GroupGrainDto>();
        if (State.Id.IsNullOrEmpty() || State.IsDelete)
        {
            result.Code = CommonResult.GroupNotExistCode;
            return result;
        }

        State.IsDelete = true;
        await WriteStateAsync();
        result.Data = _objectMapper.Map<GroupState, GroupGrainDto>(State);
        return result;
    }

    public Task<GrainResultDto<List<GroupMember>>> GetMembers()
    {
        var result = new GrainResultDto<List<GroupMember>>();
        if (State.Id.IsNullOrEmpty() || State.IsDelete)
        {
            result.Code = CommonResult.GroupNotExistCode;
            return Task.FromResult(result);
        }

        result.Data = State.Members;
        return Task.FromResult(result);
    }

    public Task<bool> Exist()
    {
        return Task.FromResult(!State.Id.IsNullOrEmpty() && !State.IsDelete);
    }

    public async Task<GrainResultDto<GroupGrainDto>> LeaveGroup(string userId)
    {
        var result = new GrainResultDto<GroupGrainDto>();
        if (State.Id.IsNullOrEmpty() || State.IsDelete)
        {
            result.Code = CommonResult.GroupNotExistCode;
            return result;
        }

        State.Members.RemoveAll(t => t.PortKeyId == userId);
        await WriteStateAsync();
        result.Data = _objectMapper.Map<GroupState, GroupGrainDto>(State);
        return result;
    }
}