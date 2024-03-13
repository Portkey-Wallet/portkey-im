using System;
using System.Collections.Generic;
using System.Linq;
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
            $"select relation_id as RelationId,is_admin as IsAdmin,`index` from im_channel_member where channel_uuid='{requestDto.ChannelUuid}' and status=0 order by `index` limit {requestDto.SkipCount}, {requestDto.MaxResultCount}; select count(*) from im_channel_member where status=0;";

        var res = await _imRepository.QueryPageAsync<MemberQueryDto>(sql);
        var members = ObjectMapper.Map<List<MemberQueryDto>, List<MemberInfo>>(res.data?.ToList());
        await _proxyChannelContactAppService.BuildUserNameAsync(members, null);

        return new MembersInfoResponseDto
        {
            Members = members,
            TotalCount = res.totalCount
        };
    }

    public async Task<List<MemberInfo>> SearchMembersAsync(SearchMembersRequestDto requestDto)
    {
        var keyword = requestDto.Keyword.Trim();
        var result = new List<MemberInfo>();

        // userId
        if (Guid.TryParse(keyword, out var userId))
        {
            var userInfo = await _userProvider.GetUserInfoByIdAsync(userId);
            if (userInfo == null)
            {
                return result;
            }

            var imUserInfo = await GetMemberAsync(requestDto.ChannelUuid, userInfo.RelationId);
            result.Add(imUserInfo);
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
            result.Add(imUserInfo);
            return result;
        }

        // // name or remark
        // var currentUserId = CurrentUser.GetId();
        // //get all contact name or remark
        // var contactProfileDtos = await GetContactListAsync(new List<Guid>(), string.Empty);
        // var contacts = contactProfileDtos?.Where(t => t.Name == keyword || t.CaHolderInfo.WalletName == keyword)
        //     .ToList();
        // if (contacts.IsNullOrEmpty())
        // {
        //     return result;
        // }
        //
        // // get all relation id from db where groupid
        // // get userindex where relation ids
        // // name
        // var relationIds = contacts.Select(t => t.CaHolderInfo.UserId.ToString()).ToList();
        //
        // var c_members = await GetMembersAsync(requestDto.ChannelUuid,relationIds);
        //
        //
        // result.AddRange(c_members);
        return result;
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
        var sql =
            $"select relation_id as RelationId,is_admin as IsAdmin from im_channel_member where channel_uuid='{channelUuid}' and status=0 and relation_id in '{relationIds}' limit 1;";
        var imUserInfo = await _imRepository.QueryAsync<MemberInfo>(sql);
        await _proxyChannelContactAppService.BuildUserNameAsync(imUserInfo.ToList(), null);

        return imUserInfo?.ToList();
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