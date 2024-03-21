using System.Collections.Generic;
using System.Threading.Tasks;
using IM.ChannelContact.Dto;

namespace IM.ChannelContact;

public interface IChannelContactV2AppService
{
    Task<MembersInfoResponseDto> GetChannelMembersAsync(ChannelMembersRequestDto requestDto);
    Task<ChannelDetailResponseDto> GetChannelDetailInfoAsync(ChannelDetailInfoRequestDto requestDto);
    Task<MembersInfoResponseDto> SearchMembersAsync(SearchMembersRequestDto requestDto);
}