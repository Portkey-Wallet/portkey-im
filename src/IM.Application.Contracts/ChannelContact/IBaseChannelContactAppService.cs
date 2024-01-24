using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IM.ChannelContact.Dto;

namespace IM.ChannelContact;

public interface IBaseChannelContactAppService
{
    Task<CreateChannelResponseDto> CreateChannelAsync(CreateChannelRequestDto requestDto);
    Task<ChannelDetailInfoResponseDto> GetChannelDetailInfoAsync(ChannelDetailInfoRequestDto requestDto);
    Task<List<MemberInfo>> GetChannelMembersAsync(ChannelMembersRequestDto requestDto);
    Task<string> JoinChannelAsync(JoinChannelRequestDto joinChannelRequestDto);

    Task<string> RemoveFromChannelAsync(RemoveMemberRequestDto removeMemberRequestDto);

    Task<string> LeaveChannelAsync(LeaveChannelRequestDto leaveChannelRequestDto);

    Task<string> DisbandChannelAsync(DisbandChannelRequestDto disbandChannelRequestDto);

    Task<string> ChannelOwnerTransferAsync(OwnerTransferRequestDto ownerTransferRequestDto);

    Task<bool> IsAdminAsync(string id);

    Task<AnnouncementResponseDto> ChannelAnnouncementAsync(ChannelAnnouncementRequestDto requestDto);

    Task<string> SetChannelAnnouncementAsync(ChannelSetAnnouncementRequestDto requestDto);
    
    Task<string> SetChannelNameAsync(SetChannelNameRequestDto requestDto);
    
    Task<Object> AddChannelMemberAsync(ChannelAddMemeberRequestDto requestDto);

}