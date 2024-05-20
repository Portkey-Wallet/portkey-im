using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using IM.User;
using IM.User.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp;

namespace IM.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ImUser")]
[Route("api/v1/users")]
// [Authorize] todo remove before online
public class UserController : ImController
{
    private readonly IUserAppService _userAppService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserAppService userAppService, ILogger<UserController> logger)
    {
        _userAppService = userAppService;
        _logger = logger;
    }

    [HttpPost("token")]
    public async Task<SignatureDto> GetSignatureAsync(SignatureRequestDto input)
    {
        return await _userAppService.GetSignatureAsync(input);
    }

    [HttpPost("auth"), Authorize]
    public async Task<SignatureDto> GetAuthTokenAsync(AuthRequestDto input)
    {
        return await _userAppService.GetAuthTokenAsync(input);
    }

    [HttpGet("userInfo"), Authorize]
    public async Task<UserInfoDto> GetUserInfoAsync(UserInfoRequestDto input)
    {
        return await _userAppService.GetUserInfoAsync(input);
    }

    [HttpGet("imUserInfo")]
    public async Task<ImUserDto> GetImUserInfoAsync([Required] string relationId)
    {
        return await _userAppService.GetImUserInfoAsync(relationId);
    }
    
    [HttpGet("imUser")]
    public async Task<ImUserDto> GetImUserAsync([Required] string address)
    {
        return await _userAppService.GetImUserAsync(address);
    }

    [HttpGet("addresses")]
    public async Task<List<AddressInfoDto>> GetAddressesAsync([Required] string relationId)
    {
        return await _userAppService.GetAddressesAsync(relationId);
    }

    [HttpPost("report")]
    public async Task<string> ReportUserImMessage(ReportUserImMessageCmd reportUserImMessageCmd)
    {
        _logger.LogInformation("report user api input parameters={0}", JsonConvert.SerializeObject(reportUserImMessageCmd));
        await _userAppService.ReportUserImMessage(reportUserImMessageCmd);
        return "success";
    }
    
    [HttpPost("userInfo/update"), Authorize]
    public async Task<string> UpdateImUserAsync(ImUsrUpdateDto input)
    {
        await _userAppService.UpdateImUserAsync(input);
        return "success";
    }
    
    [HttpGet("userInfo/list"), Authorize]
    public async Task<List<UserInfoListDto>> ListUserInfoAsync(UserInfoListRequestDto input)
    {
        return await _userAppService.ListUserInfoAsync(input);
    }
}