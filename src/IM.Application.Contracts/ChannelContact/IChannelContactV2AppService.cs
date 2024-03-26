using System.Threading.Tasks;
using IM.ChannelContact.Dto;

namespace IM.ChannelContact;

public interface IChannelContactV2AppService
{
    Task<MembersInfoResponseDto> GetChannelMembersAsync(ChannelMembersRequestDto requestDto);
    Task<ChannelDetailResponseDto> GetChannelDetailInfoAsync(ChannelDetailInfoRequestDto requestDto);
    Task<MembersInfoResponseDto> SearchMembersAsync(SearchMembersRequestDto requestDto);
    Task<ContactResultDto> GetContactsAsync(ContactRequestDto requestDto);
}