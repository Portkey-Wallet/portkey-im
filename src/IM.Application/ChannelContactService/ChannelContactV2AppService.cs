using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf;
using IM.ChannelContact;
using IM.ChannelContact.Dto;
using IM.Common;
using IM.Commons;
using IM.Contact.Dtos;
using IM.Dapper.Repository;
using IM.Options;
using IM.User.Provider;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Users;

namespace IM.ChannelContactService;

[RemoteService(false), DisableAuditing]
public class ChannelContactV2AppService : ImAppService, IChannelContactV2AppService
{
    private readonly IImRepository _imRepository;
    private readonly IProxyChannelContactAppService _proxyChannelContactAppService;
    private readonly IUserProvider _userProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly CAServerOptions _caServerOptions;

    public ChannelContactV2AppService(IImRepository imRepository,
        IProxyChannelContactAppService proxyChannelContactAppService, IUserProvider userProvider,
        IHttpContextAccessor httpContextAccessor, IHttpClientProvider httpClientProvider,
        IOptionsSnapshot<CAServerOptions> caServerOptions)
    {
        _imRepository = imRepository;
        _proxyChannelContactAppService = proxyChannelContactAppService;
        _userProvider = userProvider;
        _httpContextAccessor = httpContextAccessor;
        _httpClientProvider = httpClientProvider;
        _caServerOptions = caServerOptions.Value;
    }

    public async Task<ChannelDetailResponseDto> GetChannelDetailInfoAsync(ChannelDetailInfoRequestDto requestDto)
    {
        var userInfo = await _userProvider.GetUserInfoByIdAsync(CurrentUser.GetId());
        if (userInfo == null)
        {
            throw new UserFriendlyException("user not exist.");
        }

        var sql =
            "select channel.uuid,channel.name,channel.icon,channel.announcement,channel.pin_announcement as PinAnnouncement,channel.open_access as OpenAccess,channel.type,im_user.mute,im_user.pin from im_channel channel join " +
            $"im_user_channel im_user on channel.uuid=im_user.channel_uuid where im_user.relation_id = '{userInfo.RelationId}' and channel.uuid = '{requestDto.ChannelUuid}' and channel.status=0;";

        var channelDetail = await _imRepository.QueryFirstOrDefaultAsync<ChannelDetailResponseDto>(sql);

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
        var sql =
            $"select relation_id as RelationId,is_admin as IsAdmin,`index` from im_channel_member where channel_uuid='{requestDto.ChannelUuid}' and status=0 order by `index` limit {requestDto.SkipCount}, {requestDto.MaxResultCount}; select count(*) from im_channel_member where channel_uuid='{requestDto.ChannelUuid}' and status=0;";

        var res = await _imRepository.QueryPageAsync<MemberQueryDto>(sql);
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
        // userId
        if (Guid.TryParse(keyword, out var userId))
        {
            var userInfo = await _userProvider.GetUserInfoByIdAsync(userId);
            if (userInfo == null)
            {
                return result;
            }

            var imUserInfo = await GetMemberAsync(requestDto.ChannelUuid, userInfo.RelationId);
            result.Members.Add(imUserInfo);
            return result;
        }

        // address
        var isAddress = CheckIsAddress(keyword);
        if (isAddress)
        {
            var user = await _userProvider.GetUserInfoAsync(Guid.Empty, keyword);
            if (user == null)
            {
                return result;
            }

            var imUserInfo = await GetMemberAsync(requestDto.ChannelUuid, user.RelationId);
            result.Members.Add(imUserInfo);
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

        if (!requestDto.FilteredMember.IsNullOrWhiteSpace() &&
            memInfo.FirstOrDefault(t => t.RelationId == requestDto.FilteredMember) != null)
        {
            result.Members.RemoveAll(t => t.RelationId == requestDto.FilteredMember);
            result.TotalCount -= 1;
        }

        return result;
    }

    public async Task<ContactResultDto> GetContactsAsync(ContactRequestDto requestDto)
    {
        var result = new ContactResultDto();
        var contacts = new List<ContactDto>();
        var currentUserId = CurrentUser.GetId();
        //get all contact name or remark
        var contactDtos = await GetContactsAsync(currentUserId);
        contactDtos = contactDtos.Where(t => t.ImInfo != null && t.CaHolderInfo != null).ToList();
        if (!requestDto.Keyword.IsNullOrWhiteSpace())
        {
            var keyword = requestDto.Keyword.Trim();
            if (Guid.TryParse(keyword, out var userId))
            {
                var userInfo = contactDtos.FirstOrDefault(t => t.CaHolderInfo.UserId == userId);
                if (userInfo == null)
                {
                    return result;
                }

                contacts.Add(userInfo);
                result.Contacts = contacts;
                result.TotalCount = contacts.Count;
                return result;
            }

            // address
            var isAddress = CheckIsAddress(keyword);
            if (isAddress)
            {
                var user = contactDtos.FirstOrDefault(t => t.Addresses.Any(f => f.Address == keyword));
                if (user == null)
                {
                    return result;
                }

                contacts.Add(user);
                result.Contacts = contacts;
                result.TotalCount = contacts.Count;
                return result;
            }
        }

        var relationIds = contactDtos.Select(t => t.ImInfo.RelationId).ToList();
        var members = await GetMembersAsync(requestDto.ChannelUuid, relationIds);
        if (members.IsNullOrEmpty())
        {
            return result;
        }

        var contactMembers = contactDtos.Where(t => members.Select(f => f.RelationId).Contains(t.ImInfo.RelationId))
            .ToList();
        contactMembers.ForEach(t => t.IsGroupMember = true);

        if (!requestDto.Keyword.IsNullOrWhiteSpace())
        {
            var keyword = requestDto.Keyword.Trim();
            contactMembers = contactMembers.Where(t => t.Name.ToUpper().Contains(keyword.ToUpper())).ToList();
        }

        result.Contacts = contactMembers.Skip(requestDto.SkipCount).Take(requestDto.MaxResultCount).ToList();
        result.TotalCount = contactMembers.Count;

        return result;
    }

    private async Task<List<ContactDto>> GetContactsAsync(Guid userId)
    {
        var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
            out var authToken);

        var header = new Dictionary<string, string>();
        if (hasAuthToken)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        var url = $"{_caServerOptions.BaseUrl}{CAServerConstant.GetContactsByUserId}?userId={userId.ToString()}";
        return await _httpClientProvider.GetAsync<List<ContactDto>>(url, header);
    }

    private async Task<List<ContactProfileDto>> GetContactListAsync(List<Guid> contactIds, string keywords)
    {
        var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
            out var authToken);

        var header = new Dictionary<string, string>();
        if (hasAuthToken)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        var input = new ContactListRequestDto
        {
            ContactUserIds = contactIds,
            Address = keywords
        };
        return await _httpClientProvider.PostAsync<List<ContactProfileDto>>(
            _caServerOptions.BaseUrl + CAServerConstant.GetContactsRemark, input, header);
    }

    private async Task<MemberInfo> GetMemberAsync(string channelUuid, string relationId)
    {
        var sql =
            $"select relation_id as RelationId,is_admin as IsAdmin from im_channel_member where channel_uuid='{channelUuid}' and status=0 and relation_id='{relationId}' limit 1;";
        var imUserInfo = await _imRepository.QueryFirstOrDefaultAsync<MemberInfo>(sql);
        await _proxyChannelContactAppService.BuildUserNameAsync(new List<MemberInfo>() { imUserInfo }, null);

        return imUserInfo;
    }

    private async Task<List<MemberInfo>> GetMembersAsync(string channelUuid, List<string> relationIds)
    {
        var builder = new StringBuilder("(");
        foreach (var relationId in relationIds)
        {
            builder.Append($"'{relationId}',");
        }

        var inStr = builder.ToString();
        inStr = inStr.TrimEnd(',');
        inStr += ")";

        var sql =
            $"select relation_id as RelationId,is_admin as IsAdmin from im_channel_member where channel_uuid='{channelUuid}' and status=0 and relation_id in {inStr};";
        var imUserInfo = await _imRepository.QueryAsync<MemberInfo>(sql);
        var userList = imUserInfo?.ToList();
        if (userList.IsNullOrEmpty()) return new List<MemberInfo>();

        await _proxyChannelContactAppService.BuildUserNameAsync(userList, null);

        return userList;
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
}