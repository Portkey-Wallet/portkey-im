using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Common;
using IM.Commons;
using IM.User.Dtos;
using Volo.Abp;
using Volo.Abp.Auditing;

namespace IM.RelationOne;

[RemoteService(false), DisableAuditing]
public class ProxyUserAppService : ImAppService, IProxyUserAppService
{
    private readonly IProxyRequestProvider _proxyRequestProvider;

    public ProxyUserAppService(IProxyRequestProvider proxyRequestProvider)
    {
        _proxyRequestProvider = proxyRequestProvider;
    }

    public async Task<SignatureDto> GetSignatureAsync(SignatureRequestDto input)
    {
        return await _proxyRequestProvider.PostAsync<SignatureDto>(
            ImUrlConstant.AddressToken, input);
    }

    public async Task<SignatureDto> GetAuthTokenAsync(AuthRequestDto input)
    {
        try
        {
            var header = new Dictionary<string, string>()
                { { RelationOneConstant.GetTokenHeader, $"{CommonConstant.JwtPrefix} {input.AddressAuthToken}" } };

            return await _proxyRequestProvider.PostAsync<SignatureDto>(
                ImUrlConstant.AuthToken, input, header);
        }
        catch (UserFriendlyException e) // The front end proposes to make this change
        {
            if (!RelationOneConstant.ImTokenErrorMappings.ContainsKey(e.Code)) throw;
            
            throw new UserFriendlyException(e.Message, RelationOneConstant.ImTokenErrorMappings[e.Code].Code);
        }
    }

    public async Task<UserInfoDto> GetUserInfoAsync(UserInfoRequestDto input)
    {
        var fields = HttpHelper.StringArrayToGetParam(nameof(input.Fields).ToLower(), input.Fields);
        return await _proxyRequestProvider.GetAsync<UserInfoDto>(
            $"{ImUrlConstant.UserInfo}?address={input.Address}&fields=CREATE_AT{fields}");
    }

    public async Task<List<AddressInfoDto>> GetAddressesAsync(string relationId)
    {
        return await _proxyRequestProvider.GetAsync<List<AddressInfoDto>>(
            $"{ImUrlConstant.AddressList}?relationId={relationId}");
    }

    public async Task UpdateImUserAsync(ImUsrUpdateDto input)
    {
        await _proxyRequestProvider.PostAsync<object>(ImUrlConstant.UpdateImUser, input);
    }

    public async Task<List<UserInfoListDto>> ListUserInfoAsync(UserInfoListRequestDto input)
    {
        return await _proxyRequestProvider.GetAsync<List<UserInfoListDto>>(
            $"{ImUrlConstant.SearchUserInfo}?keywords={input.Keywords}");
        
    }
}