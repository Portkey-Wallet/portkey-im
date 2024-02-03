using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using AElf;
using IM.Common;
using IM.Commons;
using IM.Contact.Dtos;
using IM.Entities.Es;
using IM.Grains.Grain;
using IM.Grains.Grain.User;
using IM.Options;
using IM.RelationOne;
using IM.User.Dtos;
using IM.User.Etos;
using IM.User.Provider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Orleans;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Users;

namespace IM.User;

[RemoteService(false), DisableAuditing]
public class UserAppService : ImAppService, IUserAppService
{
    //CAHolderIndex
    private readonly IProxyUserAppService _proxyUserAppService;
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IClusterClient _clusterClient;
    private readonly IUserProvider _userProvider;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly CAServerOptions _caServerOptions;

    public UserAppService(IProxyUserAppService proxyUserAppService,
        IDistributedEventBus distributedEventBus,
        IClusterClient clusterClient,
        IHttpClientProvider httpClientProvider,
        IUserProvider userProvider,
        IHttpContextAccessor httpContextAccessor,
        IOptionsSnapshot<CAServerOptions> caServerOptions)
    {
        _proxyUserAppService = proxyUserAppService;
        _distributedEventBus = distributedEventBus;
        _clusterClient = clusterClient;
        _httpClientProvider = httpClientProvider;
        _userProvider = userProvider;
        _httpContextAccessor = httpContextAccessor;
        _caServerOptions = caServerOptions.Value;
    }

    public async Task<SignatureDto> GetSignatureAsync(SignatureRequestDto input)
    {
        return await _proxyUserAppService.GetSignatureAsync(input);
    }

    [Authorize]
    public async Task<SignatureDto> GetAuthTokenAsync(AuthRequestDto input)
    {
        var holder = await GetCaHolderAsync(CurrentUser.GetId());
        input.Name = holder?.Nickname ?? CurrentUser.GetId().ToString("N").Substring(0, 8);

        var authToken = await _proxyUserAppService.GetAuthTokenAsync(input);
        if (authToken == null || authToken.Token.IsNullOrWhiteSpace()) return authToken;

        var addResult = await AddGrainAsync(CurrentUser.GetId(), authToken.Token, input.Name);
        if (addResult == null || !addResult.Success() || addResult.Data == null) return authToken;

        await _distributedEventBus.PublishAsync(ObjectMapper.Map<UserGrainDto, AddUserEto>(addResult.Data));
        return authToken;
    }

    public async Task<UserInfoDto> GetUserInfoAsync(UserInfoRequestDto input)
    {
        if (input.Address.IsNullOrWhiteSpace())
        {
            return await _proxyUserAppService.GetUserInfoAsync(input);
        }

        if (_httpContextAccessor.HttpContext != null && _httpContextAccessor.HttpContext.Request.Headers.Keys.Contains(
                RelationOneConstant.AuthHeader,
                StringComparer.OrdinalIgnoreCase))
        {
            _httpContextAccessor.HttpContext.Request.Headers[RelationOneConstant.AuthHeader] = string.Empty;
        }

        var address = input.Address;
        if (Guid.TryParse(input.Address, out var portKeyId))
        {
            address = string.Empty;
        }

        var user = await _userProvider.GetUserInfoAsync(portKeyId, address);

        input.Address = address.IsNullOrWhiteSpace() ? user == null ? string.Empty : user.RelationId : address;
        var userInfo = await _proxyUserAppService.GetUserInfoAsync(input);

        if (user != null)
        {
            userInfo.PortkeyId = user.Id.ToString();

            // contact name
            var contact = await GetContactAsync(user.Id);
            if (contact != null)
            {
                var name = contact.Name;
                if (name.IsNullOrWhiteSpace())
                {
                    name = contact.CaHolderInfo?.WalletName;
                }

                if (!name.IsNullOrWhiteSpace())
                {
                    userInfo.Name = name;
                }
            }
        }

        return userInfo;
    }

    public async Task<List<AddressInfoDto>> GetAddressesAsync(string relationId) =>
        await _proxyUserAppService.GetAddressesAsync(relationId);

    public async Task<ImUserDto> GetImUserInfoAsync(string relationId)
    {
        var userInfo = await _proxyUserAppService.GetUserInfoAsync(new UserInfoRequestDto()
            { Address = relationId, Fields = new List<string> { "ADDRESS_WITH_CHAIN" } });
        var imUserDto = ObjectMapper.Map<UserInfoDto, ImUserDto>(userInfo);
        var user = await _userProvider.GetUserInfoAsync(relationId);
        if (imUserDto != null && user != null)
        {
            imUserDto.PortkeyId = user.Id;
        }

        return imUserDto;
    }

    public async Task<ImUserDto> GetImUserAsync(string address)
    {
        var user = await _userProvider.GetUserInfoAsync(Guid.Empty, address);
        return ObjectMapper.Map<UserIndex, ImUserDto>(user);
    }

    private async Task<GrainResultDto<UserGrainDto>> AddGrainAsync(Guid userId, string token, string name)
    {
        var userGrain = _clusterClient.GetGrain<IUserGrain>(userId);
        var existed = await userGrain.Exist();
        if (!existed)
        {
            return await userGrain.AddUser(await GetUserInfoAsync(userId, token, name));
        }

        var needUpdate = await userGrain.NeedUpdate();
        Logger.LogInformation("is im user need to update: {needUpdate}, {userId}", needUpdate, userId.ToString());
        if (!needUpdate) return null;

        Logger.LogInformation("im user need to update, {userId}", userId.ToString());
        //need to optimize
        var updateResult = await userGrain.UpdateUser(await GetUserInfoAsync(userId, token, name));
        Logger.LogInformation("im user update result, {result}", JsonConvert.SerializeObject(updateResult));

        return updateResult;
    }

    private async Task<UserGrainDto> GetUserInfoAsync(Guid userId, string token, string name)
    {
        var holder = await GetCaHolderAsync(userId);
        var userDto = new UserGrainDto
        {
            Id = userId,
            Name = name,
            CaHash = holder.CaHash,
            CaAddresses = new List<CaAddressInfo>()
        };

        var holderInfo = await _userProvider.GetCaHolderInfoAsync(holder.CaHash);
        var addressInfo = holderInfo?.CaHolderInfo?.Select(t => new { t.ChainId, t.CaAddress }).ToList();

        if (_httpContextAccessor.HttpContext.Request.Headers.Keys.Contains(RelationOneConstant.AuthHeader,
                StringComparer.OrdinalIgnoreCase))
        {
            _httpContextAccessor.HttpContext.Request.Headers[RelationOneConstant.AuthHeader] =
                $"{CommonConstant.JwtPrefix} {token}";
        }
        else
        {
            _httpContextAccessor.HttpContext?.Request.Headers.Add(RelationOneConstant.AuthHeader,
                $"{CommonConstant.JwtPrefix} {token}");
        }

        var userInfo = await _proxyUserAppService.GetUserInfoAsync(new UserInfoRequestDto
            { Address = string.Empty, Fields = new List<string> { "ADDRESS_WITH_CHAIN" } });

        userDto.RelationId = userInfo.RelationId;
        if (userInfo?.AddressWithChain is not { Count: > 0 } ||
            addressInfo == null)
        {
            Logger.LogError(
                "userDto.AddressWithChain not exist, userId:{userId},tokenHash:{tokenHash},relationId:{relationId}",
                userId, HashHelper.ComputeFrom(token).ToHex(), userDto.RelationId);
            throw new UserFriendlyException("userInfo.AddressWithChain not exist");
        }

        addressInfo?.Where(t => userInfo.AddressWithChain.Select(f => f.Address).Contains(t.CaAddress))
            .ToList().ForEach(t => userDto.CaAddresses.Add(new CaAddressInfo
            {
                ChainId = t.ChainId,
                Address = t.CaAddress
            }));

        if (userDto.CaAddresses == null || userDto.CaAddresses.Count == 0)
        {
            Logger.LogError(
                "userDto.CaAddresses not match, userId:{userId}, tokenHash:{tokenHash}, relationId:{relationId}",
                userId, HashHelper.ComputeFrom(token).ToHex(), userDto.RelationId);
            throw new UserFriendlyException("userDto.CaAddresses not exist");
        }

        return userDto;
    }

    private async Task<CAHolderDto> GetCaHolderAsync(Guid userId)
    {
        var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
            out var authToken);

        var header = new Dictionary<string, string>();
        if (hasAuthToken)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        return await _httpClientProvider.GetAsync<CAHolderDto>(_caServerOptions.BaseUrl + "api/app/account/caHolder",
            header);
    }

    public async Task<ContactProfileDto> GetContactAsync(Guid contactUserId)
    {
        var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
            out var authToken);


        var header = new Dictionary<string, string>();
        if (hasAuthToken)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        return await _httpClientProvider.GetAsync<ContactProfileDto>(
            _caServerOptions.BaseUrl + $"api/app/contacts/getContact?contactUserId={contactUserId}",
            header);
    }

    public async Task<List<CAUserDto>> GetCaHolderAsync(List<Guid> userIds, string token = null)
    {
        var authToken = new StringValues();
        Debug.Assert((_httpContextAccessor.HttpContext != null || !string.IsNullOrEmpty(token)),
            "_httpContextAccessor.HttpContext != null");
        var hasAuthToken = _httpContextAccessor.HttpContext?.Request?.Headers.TryGetValue(CommonConstant.AuthHeader,
            out authToken);
        if (!token.IsNullOrEmpty())
        {
            authToken = token;
            hasAuthToken = true;
        }

        var header = new Dictionary<string, string>();
        if (hasAuthToken == true)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        return await _httpClientProvider.PostAsync<List<CAUserDto>>(_caServerOptions.BaseUrl + "api/app/imUsers/names",
            userIds, header);
    }

    public async Task<List<UserInfoListDto>> ListUserInfoAsync(UserInfoListRequestDto input)
    {
        //get holder infos by email/phone/google/apple
        var portKeyIds = await GetHolderInfosAsync(input.Keywords);

        if (Guid.TryParse(input.Keywords, out var portKeyId) && portKeyId != CurrentUser.GetId())
        {
            portKeyIds.Add(portKeyId);
        }

        var currentUser = await _userProvider.GetUserInfoByIdAsync(CurrentUser.GetId());
        var count = currentUser?.CaAddresses.Where(a => a.Address == input.Keywords).ToList().Count;
        var keywords = count == 0 ? input.Keywords : "";

        //get relation ids by portkey ids or caAddress, then deduplicate
        var userList = await _userProvider.ListUserInfoAsync(portKeyIds, keywords);
        var users = userList
            .GroupBy(user => user.RelationId)
            .Select(group => group.First())
            .Where(u => u.CaAddresses.Where(a => a.Address == input.Keywords).ToList().Count == 0)
            .ToList();

        //search user info by relation id
        var tasks = users.Select(user => SearchUserInfoAsync(user.RelationId, user.Id)).ToList();

        //search user info by otherAddress or wallet name

        if (count == 0)
        {
            tasks.Add(SearchUserInfoAsync(input.Keywords, Guid.Empty));
        }

        var result = await Task.WhenAll(tasks);
        var all = new List<UserInfoListDto>();
        foreach (var dto in result.Where(x => x != null))
        {
            all.AddRange(dto.ToList());
        }

        // remove self
        all.RemoveAll(t => t.RelationId == currentUser?.RelationId);
        //get contact's remark
        var contactProfileDtos = await GetContactListAsync(portKeyIds, input.Keywords);

        var map = contactProfileDtos.ToDictionary(i => i.ImInfo.RelationId, i => i);

        var needAddAvatarUsers = new List<UserInfoListDto>();
        foreach (var userInfo in all)
        {
            var contact = map.GetOrDefault(userInfo.RelationId);
            if (contact == null)
            {
                needAddAvatarUsers.Add(userInfo);
                continue;
            }

            var name = contact.Name;
            if (name.IsNullOrWhiteSpace())
            {
                name = contact.CaHolderInfo?.WalletName;
            }

            if (!name.IsNullOrWhiteSpace())
            {
                userInfo.Name = name;
            }

            if (userInfo.Avatar.IsNullOrWhiteSpace())
            {
                userInfo.Avatar = contact.Avatar;
            }
        }

        await AddAvatarAsync(needAddAvatarUsers);
        return all
            .GroupBy(user => user.RelationId)
            .Select(group => group.First())
            .ToList();
    }

    private async Task AddAvatarAsync(List<UserInfoListDto> userInfos)
    {
        if (userInfos.IsNullOrEmpty()) return;

        var portKeyIds = userInfos.Where(t => !t.PortkeyId.IsNullOrWhiteSpace()).Select(f => f.PortkeyId).ToList();
        if (portKeyIds.IsNullOrEmpty()) return;

        var holderList = await GetHoldersAsync(portKeyIds);
        foreach (var userInfo in userInfos.Where(t => !t.PortkeyId.IsNullOrWhiteSpace()))
        {
            userInfo.Avatar = holderList?.FirstOrDefault(t => t.UserId.ToString() == userInfo.PortkeyId)?.Avatar;
        }
    }

    private async Task<List<HolderInfoResultDto>> GetHoldersAsync(List<string> ids)
    {
        var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
            out var authToken);


        var header = new Dictionary<string, string>();
        if (hasAuthToken)
        {
            header.Add(CommonConstant.AuthHeader, authToken);
        }

        var queryBuilder = new StringBuilder("?");
        foreach (var id in ids)
        {
            queryBuilder.Append($"userIds={id}&");
        }

        var queryString = queryBuilder.ToString().TrimEnd('&');
        return await _httpClientProvider.GetAsync<List<HolderInfoResultDto>>(
            _caServerOptions.BaseUrl + CAServerConstant.HolderInfos + queryString, header);
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

    private async Task<List<UserInfoListDto>> SearchUserInfoAsync(string keywords, Guid portkeyId)
    {
        var request = new UserInfoListRequestDto
        {
            Keywords = keywords
        };
        var userInfos = await _proxyUserAppService.ListUserInfoAsync(request);
        if (portkeyId != Guid.Empty)
        {
            userInfos.ForEach(u => u.PortkeyId = portkeyId.ToString());
        }

        return userInfos;
    }

    private async Task<List<Guid>> GetHolderInfosAsync(string keyword)
    {
        if (!VerifyHelper.IsEmail(keyword) && !VerifyHelper.IsPhone(keyword))
        {
            return new List<Guid>();
        }

        try
        {
            var hasAuthToken = _httpContextAccessor.HttpContext.Request.Headers.TryGetValue(CommonConstant.AuthHeader,
                out var authToken);

            var headers = new Dictionary<string, string>();
            if (hasAuthToken)
            {
                headers.Add(CommonConstant.AuthHeader, authToken);
            }

            var queryString = new StringBuilder();
            queryString.Append("?keyword=").Append(UrlEncoder.Default.Encode(keyword));

            return await _httpClientProvider.GetAsync<List<Guid>>(
                _caServerOptions.BaseUrl + CAServerConstant.HolderInfoList + queryString, headers);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "get holder infos fail, keyword: {keyword}", keyword);
        }

        return new List<Guid>();
    }

    public async Task UpdateImUserAsync(ImUsrUpdateDto input)
    {
        // update name and avatar.
        await UpdateUserAsync(CurrentUser.GetId(), input.Name, input.Avatar);

        if (input.Name.IsNullOrWhiteSpace()) return;
        await _proxyUserAppService.UpdateImUserAsync(input);
    }

    private async Task UpdateUserAsync(Guid id, string walletName, string avatar)
    {
        await _userProvider.UpdateUserInfoAsync(id, walletName, avatar);
    }
}