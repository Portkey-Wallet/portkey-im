using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IM.ChatBot;
using IM.Common;
using IM.Commons;
using IM.Contact.Dtos;
using IM.Options;
using IM.RelationOne;
using IM.RelationOne.Dtos.Contact;
using IM.User;
using IM.User.Dtos;
using IM.User.Provider;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace IM.Contact;

[RemoteService(false), DisableAuditing]
public class ContactAppService : ImAppService, IContactAppService
{
    private readonly IHttpClientProvider _httpClientProvider;
    private readonly IProxyContactAppService _proxyContactAppService;
    private readonly IUserAppService _userAppService;
    private readonly IProxyUserAppService _proxyUserAppService;
    private readonly VariablesOptions _variablesOptions;
    private readonly CAServerOptions _caServerOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IUserProvider _userProvider;
    private readonly ChatBotBasicInfoOptions _chatBotBasicInfoOptions;
    private readonly ILogger<ContactAppService> _logger;


    public ContactAppService(IHttpClientProvider httpClientProvider, IProxyContactAppService proxyContactAppService,
        IUserAppService userAppService, IOptions<VariablesOptions> variablesOptions,
        IOptions<CAServerOptions> caServerOptions,
        IHttpContextAccessor httpContextAccessor,
        IProxyUserAppService proxyUserAppService,
        IUserProvider userProvider, IOptionsSnapshot<ChatBotBasicInfoOptions> chatBotBasicInfoOptions,
        ILogger<ContactAppService> logger)
    {
        _httpClientProvider = httpClientProvider;
        _proxyContactAppService = proxyContactAppService;
        _userAppService = userAppService;
        _variablesOptions = variablesOptions.Value;
        _caServerOptions = caServerOptions.Value;
        _httpContextAccessor = httpContextAccessor;
        _proxyUserAppService = proxyUserAppService;
        _userProvider = userProvider;
        _logger = logger;
        _chatBotBasicInfoOptions = chatBotBasicInfoOptions.Value;
    }

    public async Task<ContactInfoDto> GetContactProfileAsync(ContactProfileRequestDto input)
    {
        var contactProfileDto = new ContactProfileDto();

        var token = _httpContextAccessor.HttpContext?.Request.Headers[CommonConstant.AuthHeader];
        var headers = new Dictionary<string, string>
        {
            { CommonConstant.AuthHeader, token }
        };

        if (input.Id != Guid.Empty)
        {
            contactProfileDto = await GetContactByContactIdAsync(input.Id, headers);
            if (contactProfileDto.ImInfo.RelationId == _chatBotBasicInfoOptions.RelationId)
            {
                return new ContactInfoDto
                {
                    Id = contactProfileDto.Id.ToString(),
                    Name = contactProfileDto.Name,
                    Avatar = _chatBotBasicInfoOptions.Avatar,
                    UserId = contactProfileDto.UserId.ToString(),
                    ImInfo = new ImInfoDto
                    {
                        Name = _chatBotBasicInfoOptions.Name,
                        RelationId = _chatBotBasicInfoOptions.RelationId,
                        PortkeyId = contactProfileDto.ImInfo.PortkeyId
                    },
                    ContactType = 1
                };
            }
        }
        else if (input.PortkeyId != Guid.Empty)
        {
            contactProfileDto = await GetContactByPortkeyIdAsync(input.PortkeyId, headers);
            if (contactProfileDto.ImInfo.RelationId == _chatBotBasicInfoOptions.RelationId)
            {
                return new ContactInfoDto
                {
                    Id = contactProfileDto.Id.ToString(),
                    Name = contactProfileDto.Name,
                    Avatar = _chatBotBasicInfoOptions.Avatar,
                    ImInfo = new ImInfoDto
                    {
                        Name = _chatBotBasicInfoOptions.Name,
                        RelationId = _chatBotBasicInfoOptions.RelationId,
                        PortkeyId = contactProfileDto.ImInfo.PortkeyId
                    },
                    ContactType = 1
                };
            }
        }
        else
        {
            if (input.RelationId == _chatBotBasicInfoOptions.RelationId)
            {
                var result = await GetByRelationIdAsync(input.RelationId);
                return new ContactInfoDto
                {
                    Id = result.Id.ToString(),
                    Name = result.Name,
                    Index = result.Index,
                    UserId = result.UserId.ToString(),
                    Avatar = _chatBotBasicInfoOptions.Avatar,
                    ImInfo = new ImInfoDto
                    {
                        Name = result.ImInfo.Name,
                        RelationId = _chatBotBasicInfoOptions.RelationId,
                        PortkeyId = result.ImInfo.PortkeyId
                    },
                    ContactType = 1
                };
            }

            contactProfileDto = await GetContactByRelationIdAsync(input.RelationId, headers);
        }

        Logger.LogDebug(
            "contact address count:{count}, contactId:{contactId}, portkeyId:{portkeyId}, relationId:{relationId}",
            contactProfileDto.Addresses.Count, input.Id, input.PortkeyId, input.RelationId ?? string.Empty);

        var caHash = contactProfileDto.CaHolderInfo?.CaHash;
        if (contactProfileDto.Addresses.Count == CommonConstant.RegisterChainCount && !caHash.IsNullOrEmpty())
        {
            Logger.LogDebug(
                "contact have only one address, contactId:{contactId}, portkeyId:{portkeyId}, relationId:{relationId}, caHash:{caHash}",
                contactProfileDto.Id, input.PortkeyId, input.RelationId ?? string.Empty, caHash);

            var holderInfo = await _userProvider.GetCaHolderInfoAsync(caHash);
            contactProfileDto.Addresses =
                ObjectMapper.Map<List<GuardianDto>, List<ContactAddressDto>>(holderInfo.CaHolderInfo);
        }

        var imageMap = _variablesOptions.ImageMap;

        foreach (var contactAddressDto in contactProfileDto.Addresses.Where(contactAddressDto =>
                     !contactAddressDto.ChainName.IsNullOrWhiteSpace()))
        {
            contactAddressDto.Image = imageMap.GetOrDefault(contactAddressDto.ChainName?.ToLowerInvariant());
        }

        contactProfileDto.Addresses = contactProfileDto.Addresses.OrderBy(t => t.ChainId).ToList();
        return ObjectMapper.Map<ContactProfileDto, ContactInfoDto>(contactProfileDto);
    }

    private async Task<List<PermissionSetting>> GetPermissionsAsync(string userId, Dictionary<string, string> headers)
    {
        try
        {
            var privacyPermissionAsyncResponseDto =
                await _httpClientProvider.GetAsync<GetPrivacyPermissionAsyncResponseDto>(
                    _caServerOptions.BaseUrl + CAServerConstant.PrivacyPermissionGet + userId, headers);
            var loginAccounts = privacyPermissionAsyncResponseDto.Permissions;
            return loginAccounts;
        }
        catch (Exception e)
        {
            return new List<PermissionSetting>();
        }
    }

    private async Task<ContactProfileDto> GetContactByContactIdAsync(Guid contactId, Dictionary<string, string> headers)
    {
        var contactProfileDto = await _httpClientProvider.GetAsync<ContactProfileDto>(
            _caServerOptions.BaseUrl + CAServerConstant.ContactsGet + contactId, headers);

        if (contactProfileDto.CaHolderInfo == null || contactProfileDto.CaHolderInfo.UserId == Guid.Empty)
        {
            return contactProfileDto;
        }

        contactProfileDto.LoginAccounts =
            await GetPermissionsAsync(contactProfileDto.CaHolderInfo.UserId.ToString(), headers);

        return contactProfileDto;
    }

    private async Task<ContactProfileDto> GetContactByPortkeyIdAsync(Guid portkeyId, Dictionary<string, string> headers)
    {
        var contactProfileDto = new ContactProfileDto();

        var contact = await _userAppService.GetContactAsync(portkeyId);
        _logger.LogDebug("query by porytkey id contact is {contact}", JsonConvert.SerializeObject(contact));

        if (contact != null)
        {
            contactProfileDto = contact;
        }

        if (contactProfileDto.Id == Guid.Empty)
        {
            var user = await _userProvider.GetUserInfoByIdAsync(portkeyId);
            var holderInfo = await GetHolderInfoAsync(portkeyId.ToString());

            contactProfileDto = ObjectMapper.Map<HolderInfoResultDto, ContactProfileDto>(holderInfo);

            contactProfileDto.CaHolderInfo = new Dtos.CaHolderInfoDto
            {
                WalletName = holderInfo.WalletName,
                UserId = holderInfo.UserId,
                CaHash = holderInfo.CaHash
            };

            contactProfileDto.ImInfo = new ImInfoDto
            {
                RelationId = user.RelationId,
                PortkeyId = portkeyId.ToString(),
            };

            contactProfileDto.Index = IndexHelper.GetIndex(holderInfo.WalletName);
            contactProfileDto.Addresses =
                ObjectMapper.Map<List<AddressResultDto>, List<ContactAddressDto>>(holderInfo.AddressInfos);

            //this means remark,stranger do not have remark
            contactProfileDto.Name = "";
        }

        if (portkeyId == Guid.Empty)
        {
            return contactProfileDto;
        }

        contactProfileDto.LoginAccounts = await GetPermissionsAsync(portkeyId.ToString(), headers);
        return contactProfileDto;
    }

    private async Task<ContactProfileDto> GetContactByRelationIdAsync(string relationId,
        Dictionary<string, string> headers)
    {
        var contactProfileDto = new ContactProfileDto();
        var loginAccounts = new List<PermissionSetting>();
        var userInfoRequestDto = new UserInfoRequestDto
        {
            Address = relationId,
            Fields = new List<string> { "ADDRESS_WITH_CHAIN" }
        };

        var user = await _userProvider.GetUserInfoAsync(relationId);
        _logger.LogDebug("query from userIndex contact is {contact}", JsonConvert.SerializeObject(user));

        var userInfo = await _proxyUserAppService.GetUserInfoAsync(userInfoRequestDto);

        if (user != null)
        {
            userInfo.PortkeyId = (user.Id == Guid.Empty ? string.Empty : user.Id.ToString());

            var contact = await _userAppService.GetContactAsync(user.Id);

            if (contact != null)
            {
                userInfo.CAName = contact.Name;
                contactProfileDto = contact;
            }

            if (!string.IsNullOrEmpty(userInfo.PortkeyId))
            {
                loginAccounts = await GetPermissionsAsync(userInfo.PortkeyId, headers);
            }
        }

        if (contactProfileDto.Id == Guid.Empty)
        {
            contactProfileDto = await StrangeContactDataAsync(userInfo);
        }

        contactProfileDto.LoginAccounts = loginAccounts;
        return contactProfileDto;
    }

    private async Task<ContactProfileDto> StrangeContactDataAsync(UserInfoDto userInfo)
    {
        var contactProfileDto = ObjectMapper.Map<UserInfoDto, ContactProfileDto>(userInfo);

        var holderInfo = await GetHolderInfoAsync(contactProfileDto.ImInfo.PortkeyId);

        if (holderInfo == null)
        {
            return contactProfileDto;
        }

        contactProfileDto.CaHolderInfo = new Dtos.CaHolderInfoDto
        {
            WalletName = holderInfo.WalletName,
            UserId = holderInfo.UserId,
            CaHash = holderInfo.CaHash
        };

        contactProfileDto.Index = IndexHelper.GetIndex(holderInfo.WalletName);
        contactProfileDto.Addresses =
            ObjectMapper.Map<List<AddressResultDto>, List<ContactAddressDto>>(holderInfo.AddressInfos);

        //this means remark,stranger do not have remark
        contactProfileDto.Name = "";
        contactProfileDto.Avatar = holderInfo.Avatar;

        return contactProfileDto;
    }

    public async Task<HolderInfoResultDto> GetHolderInfoAsync(string portkeyId)
    {
        if (string.IsNullOrWhiteSpace(portkeyId))
        {
            return null;
        }

        var token = _httpContextAccessor.HttpContext?.Request.Headers[CommonConstant.AuthHeader];

        var headers = new Dictionary<string, string>
        {
            { CommonConstant.AuthHeader, token }
        };

        var queryString = new StringBuilder();
        queryString.Append("?userId=").Append(portkeyId);

        return await _httpClientProvider.GetAsync<HolderInfoResultDto>(
            _caServerOptions.BaseUrl + CAServerConstant.HolderInfo + queryString, headers);
    }

    public async Task<PagedResultDto<ContactProfileDto>> GetListAsync(ContactGetListRequestDto input)
    {
        var queryString = new StringBuilder();
        queryString.Append("?isAbleChat=").Append(input.IsAbleChat);
        queryString.Append("&skipCount=").Append(input.SkipCount);
        queryString.Append("&maxResultCount=").Append(input.MaxResultCount);

        if (!string.IsNullOrEmpty(input.KeyWord))
        {
            queryString.Append("&keyWord=").Append(input.KeyWord);
        }

        if (input.ModificationTime != 0)
        {
            queryString.Append("&modificationTime=").Append(input.ModificationTime);
        }

        var token = _httpContextAccessor.HttpContext?.Request.Headers[CommonConstant.AuthHeader];

        var headers = new Dictionary<string, string>
        {
            { CommonConstant.AuthHeader, token }
        };

        var pagedResultDto = await _httpClientProvider.GetAsync<PagedResultDto<ContactProfileDto>>(
            _caServerOptions.BaseUrl + CAServerConstant.ContactList + queryString, headers);

        var imageMap = _variablesOptions.ImageMap;

        foreach (var contactProfileDto in pagedResultDto.Items)
        {
            foreach (var contactAddressDto in contactProfileDto.Addresses)
            {
                contactAddressDto.Image = imageMap.GetOrDefault(contactAddressDto.ChainName?.ToLowerInvariant());
            }
        }

        return pagedResultDto;
    }

    public async Task<AddStrangerResultDto> AddStrangerAsync(AddStrangerDto input)
    {
        CheckAddStrangerParam(input);
        var token = _httpContextAccessor.HttpContext?.Request.Headers[CommonConstant.AuthHeader];
        var headers = new Dictionary<string, string>
        {
            { CommonConstant.AuthHeader, token }
        };

        var addResult = await _httpClientProvider.PostAsync<AddStrangerResultDto>(
            _caServerOptions.BaseUrl + CAServerConstant.ContactsPost, input, headers);

        var followsRequestDto = new FollowsRequestDto
        {
            Address = input.RelationId
        };

        await FollowAsync(followsRequestDto);

        return addResult;
    }

    public async Task FollowAsync(FollowsRequestDto input)
    {
        await _proxyContactAppService.FollowAsync(input);
    }

    public async Task UnFollowAsync(FollowsRequestDto input)
    {
        await _proxyContactAppService.UnFollowAsync(input);
    }

    public async Task RemarkAsync(RemarkRequestDto input)
    {
        await _proxyContactAppService.RemarkAsync(input);
    }

    private void CheckAddStrangerParam(AddStrangerDto stranger)
    {
        if (stranger != null && !stranger.RelationId.IsNullOrWhiteSpace()) return;

        throw new UserFriendlyException("Invalid input");
    }

    private async Task<ContactProfileDto> GetByRelationIdAsync(string contactId)
    {
        var token = _httpContextAccessor.HttpContext?.Request.Headers[CommonConstant.AuthHeader];
        var headers = new Dictionary<string, string>
        {
            { CommonConstant.AuthHeader, token }
        };
        _logger.LogDebug("Relation is {relationId}", contactId);
        var param = new ChatBotRequestDto
        {
            RelationId = contactId,
            UserId = (Guid)CurrentUser.Id
        };
        var contactProfileDto = await _httpClientProvider.PostAsync<ContactProfileDto>(
            _caServerOptions.BaseUrl + CAServerConstant.ContactsGetByRelationId, param, headers);
        _logger.LogDebug("Query from CAServer Contact is {contact}", JsonConvert.SerializeObject(contactProfileDto));
        if (contactProfileDto.CaHolderInfo == null || contactProfileDto.CaHolderInfo.UserId == Guid.Empty)
        {
            return contactProfileDto;
        }

        contactProfileDto.LoginAccounts =
            await GetPermissionsAsync(contactProfileDto.CaHolderInfo.UserId.ToString(), headers);

        return contactProfileDto;
    }

    private async Task<ContactProfileDto> GetByPortkeyIdAsync(Guid contactId, Dictionary<string, string> headers)
    {
        var contactProfileDto = await _httpClientProvider.GetAsync<ContactProfileDto>(
            _caServerOptions.BaseUrl + CAServerConstant.ContactsGetByPortkeyId + contactId, headers);

        if (contactProfileDto.CaHolderInfo == null || contactProfileDto.CaHolderInfo.UserId == Guid.Empty)
        {
            return contactProfileDto;
        }

        contactProfileDto.LoginAccounts =
            await GetPermissionsAsync(contactProfileDto.CaHolderInfo.UserId.ToString(), headers);

        return contactProfileDto;
    }

    private Dictionary<string, string> BuildReqHeader()
    {
        var authToken = _httpContextAccessor.HttpContext?.Request.Headers[CommonConstant.AuthHeader];
        var relationAuthToken = _httpContextAccessor.HttpContext?.Request.Headers[RelationOneConstant.AuthHeader];

        var headers = new Dictionary<string, string>
        {
            { CommonConstant.AuthHeader, authToken },
            { RelationOneConstant.AuthHeader, relationAuthToken }
        };
        return headers;
    }
}