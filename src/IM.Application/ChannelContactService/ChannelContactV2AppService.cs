using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.ChannelContactService.Provider;
using IM.User.Provider;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace IM.ChannelContactService;

[RemoteService(false), DisableAuditing]
public class ChannelContactV2AppService : ImAppService, IChannelContactV2AppService
{
    private readonly IProxyChannelContactAppService _proxyChannelContactAppService;
    private readonly IUserProvider _userProvider;
    private readonly IChannelProvider _channelProvider;

    public ChannelContactV2AppService(
        IProxyChannelContactAppService proxyChannelContactAppService,
        IUserProvider userProvider,
        IChannelProvider channelProvider)
    {
        _proxyChannelContactAppService = proxyChannelContactAppService;
        _userProvider = userProvider;
        _channelProvider = channelProvider;
    }

    public async Task<ChannelDetailResponseDto> GetChannelDetailInfoAsync(ChannelDetailInfoRequestDto requestDto)
    {
        var userInfo = await _userProvider.GetUserInfoByIdAsync(CurrentUser.GetId());
        if (userInfo == null)
        {
            throw new UserFriendlyException("user not exist.");
        }

        var channelDetail =
            await _channelProvider.GetChannelDetailInfoAsync(userInfo.RelationId, requestDto.ChannelUuid);

        if (channelDetail == null)
        {
            throw new UserFriendlyException("group not exist.");
        }

        channelDetail.MemberInfos = await GetChannelMembersAsync(new ChannelMembersRequestDto()
        {
            ChannelUuid = requestDto.ChannelUuid,
            SkipCount = requestDto.SkipCount,
            MaxResultCount = requestDto.MaxResultCount
        });

        return channelDetail;
    }

    public async Task<MembersInfoResponseDto> GetChannelMembersAsync(ChannelMembersRequestDto requestDto)
    {
        var res = await _channelProvider.GetChannelMembersAsync(requestDto);
        var members = ObjectMapper.Map<List<MemberQueryDto>, List<MemberInfo>>(res.data?.ToList());
        await _proxyChannelContactAppService.BuildUserNameAsync(members, null);

        return new MembersInfoResponseDto
        {
            Members = members,
            TotalCount = res.totalCount
        };
    }

    public async Task<MembersInfoResponseDto> SearchMembersAsync(SearchMembersRequestDto requestDto)
    {
        var result = new MembersInfoResponseDto();
        if (requestDto.Keyword.IsNullOrWhiteSpace())
        {
            return await GetChannelMembersAsync(new ChannelMembersRequestDto()
            {
                ChannelUuid = requestDto.ChannelUuid,
                SkipCount = requestDto.SkipCount,
                MaxResultCount = requestDto.MaxResultCount
            });
        }

        var keyword = requestDto.Keyword.Trim();
        // userId or address
        var memberInfo = await GetMemberInfoByKeywordAsync(keyword, requestDto.ChannelUuid);
        if (memberInfo != null)
        {
            result.Members.Add(memberInfo);
            result.TotalCount = result.Members.Count;
            return result;
        }

        // name or remark
        // var currentUserId = CurrentUser.GetId();
        // //get all contact name or remark
        // var contactsDto = await GetContactsAsync(currentUserId);
        // contactsDto = contactsDto?.Where(t => t.CaHolderInfo != null && t.ImInfo != null).ToList();
        // var contacts = contactsDto?.Where(t => t.Name.ToUpper().Contains(keyword.ToUpper()) || t.CaHolderInfo.WalletName.Contains(keyword))
        //     .ToList();
        // if (contacts.IsNullOrEmpty())
        // {
        //     return result;
        // }
        //
        // // get all relation id from db where groupid
        // // get userindex where relation ids
        // var relationIds = contacts.Select(t => t.CaHolderInfo.UserId.ToString()).ToList();
        //
        // var c_members = await GetMembersAsync(requestDto.ChannelUuid, relationIds);
        // result.Members.AddRange(c_members);

        var allMembers = await GetChannelMembersAsync(new ChannelMembersRequestDto()
        {
            ChannelUuid = requestDto.ChannelUuid,
            SkipCount = requestDto.SkipCount,
            MaxResultCount = requestDto.MaxResultCount
        });


        var memInfo = allMembers.Members.Where(t => t.Name.ToUpper().Contains(keyword.ToUpper())).ToList();

        if (memInfo.IsNullOrEmpty())
        {
            return result;
        }

        result.Members.AddRange(memInfo);
        result.TotalCount = result.Members.Count;
        if (!requestDto.FilteredMember.IsNullOrWhiteSpace() &&
            memInfo.FirstOrDefault(t => t.RelationId == requestDto.FilteredMember) != null)
        {
            result.Members.RemoveAll(t => t.RelationId == requestDto.FilteredMember);
            result.TotalCount -= 1;
        }

        if (!requestDto.Keyword.IsNullOrWhiteSpace())
        {
            result.Members = result.Members.Skip(0).Take(20).ToList();
            result.TotalCount = result.Members.Count;
        }

        return result;
    }

    private async Task<MemberInfo> GetMemberInfoByKeywordAsync(string keyword, string channelUuid)
    {
        if (Guid.TryParse(keyword, out var userId))
        {
            var userInfo = await _userProvider.GetUserInfoByIdAsync(userId);
            if (userInfo == null)
            {
                return null;
            }

            return await _channelProvider.GetMemberAsync(channelUuid, userInfo.RelationId);
        }

        // address
        var address = GetAddress(keyword);
        if (!address.IsNullOrEmpty())
        {
            var user = await _userProvider.GetUserInfoAsync(Guid.Empty, address);
            if (user == null)
            {
                return null;
            }

            return await _channelProvider.GetMemberAsync(channelUuid, user.RelationId);
        }

        return null;
    }

    public async Task<ContactResultDto> GetContactsAsync(ContactRequestDto requestDto)
    {
        var result = new ContactResultDto();
        var contacts = new List<ContactDto>();
        var currentUserId = CurrentUser.GetId();
        //get all contact name or remark
        var contactDtos = await _channelProvider.GetContactsAsync(currentUserId);
        contactDtos = contactDtos.Where(t => t.ImInfo != null && t.CaHolderInfo != null).ToList();
        if (!requestDto.Keyword.IsNullOrWhiteSpace())
        {
            var contactResult = GetContactByKeyword(requestDto.Keyword.Trim(), contactDtos);
            if (contactResult.TotalCount > 0)
            {
                return contactResult;
            }
        }

        var relationIds = contactDtos.Select(t => t.ImInfo.RelationId).ToList();
        var members = await _channelProvider.GetMembersAsync(requestDto.ChannelUuid, relationIds);
        if (members.IsNullOrEmpty())
        {
            members = new List<MemberInfo>();
        }

        var contactMembers = contactDtos.Where(t => members.Select(f => f.RelationId).Contains(t.ImInfo.RelationId))
            .ToList();
        contactMembers.ForEach(t => t.IsGroupMember = true);

        if (!requestDto.Keyword.IsNullOrWhiteSpace())
        {
            var keyword = requestDto.Keyword.Trim();
            contactDtos = contactDtos.Where(t =>
                t.Name.ToUpper().Contains(keyword.ToUpper()) || t.ImInfo.Name.ToUpper().Contains(keyword.ToUpper()) ||
                t.CaHolderInfo.WalletName.ToUpper().Contains(keyword.ToUpper())).ToList();
        }


        contactDtos = SortContacts(contactDtos);
        result.Contacts = contactDtos.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList();
        result.TotalCount = contactDtos.Count;

        return result;
    }

    private ContactResultDto GetContactByKeyword(string keyword, List<ContactDto> contactDtos)
    {
        var contacts = new List<ContactDto>();

        if (Guid.TryParse(keyword, out var userId))
        {
            var userInfo = contactDtos.FirstOrDefault(t => t.CaHolderInfo.UserId == userId);
            if (userInfo != null)
            {
                contacts.Add(userInfo);
            }
        }

        // address
        var address = GetAddress(keyword);
        if (!address.IsNullOrEmpty())
        {
            var user = contactDtos.FirstOrDefault(t => t.Addresses.Any(f => f.Address == address));
            if (user != null)
            {
                contacts.Add(user);
            }
        }

        return new ContactResultDto
        {
            Contacts = contacts,
            TotalCount = contacts.Count
        };
    }

    private string GetAddress(string keyword)
    {
        try
        {
            if (!keyword.Contains('_'))
            {
                return CheckIsAddress(keyword) ? keyword : string.Empty;
            }

            var address = string.Empty;
            foreach (var item in keyword.Split('_'))
            {
                if (item.Length < address.Length)
                {
                    continue;
                }

                address = item;
            }

            return CheckIsAddress(address) ? address : string.Empty;
        }
        catch (Exception e)
        {
            Logger.LogError(e, "get address error, keyword:{keyword}", keyword);
            return string.Empty;
        }
    }

    private bool CheckIsAddress(string keyword)
    {
        if (keyword.Length <= 35)
        {
            return false;
        }

        try
        {
            return AddressHelper.VerifyFormattedAddress(keyword);
        }
        catch (Exception e)
        {
            return false;
        }
    }

    private List<ContactDto> SortContacts(List<ContactDto> contacts)
    {
        var regNum = new Regex("^[0-9]");
        var regChar = new Regex("^[a-zA-Z]");

        var charContacts = contacts.Where(t => regChar.IsMatch(t.Name)).OrderBy(t => t.Name).ToList();
        var numContacts = contacts.Where(t => regNum.IsMatch(t.Name)).OrderBy(t => t.Name).ToList();
        var otherContacts = contacts.Except(charContacts).Except(numContacts).OrderBy(t => t.Name).ToList();
        return charContacts.Union(numContacts).Union(otherContacts).ToList();
    }
}