using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using IM.ChatBot;
using IM.Options;
using IM.User;
using IM.User.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.DistributedLocking;

namespace IM.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ImUser")]
[Route("api/v1/users")]
[Authorize]
public class UserController : ImController
{
    private readonly IUserAppService _userAppService;
    private readonly IBlockUserAppService _blockUserAppService;
    private readonly ILogger<UserController> _logger;
    private IAbpDistributedLock _distributedLock;
    private readonly string _lockKeyPrefix = "Portkey:IM:ReportUser:";
    private readonly ChatBotBasicInfoOptions _chatBotBasicInfoOptions;

    public UserController(IUserAppService userAppService, IBlockUserAppService blockUserAppService,
        ILogger<UserController> logger, IAbpDistributedLock distributedLock,
        IOptionsSnapshot<ChatBotBasicInfoOptions> chatBotBasicInfoOptions)
    {
        _userAppService = userAppService;
        _blockUserAppService = blockUserAppService;
        _logger = logger;
        _distributedLock = distributedLock;
        _chatBotBasicInfoOptions = chatBotBasicInfoOptions.Value;
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
        await using var handle = await _distributedLock.TryAcquireAsync(name: _lockKeyPrefix
                                                                              + reportUserImMessageCmd.UserId + ":"
                                                                              + reportUserImMessageCmd.ReportType + ":"
                                                                              + reportUserImMessageCmd.MessageId);
        if (handle == null)
        {
            return "failed reason: reporting too frequently";
        }

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
        var result = await _userAppService.ListUserInfoAsync(input);
        var headers = Request.Headers;
        var platform = headers["platform"];
        var version = headers["version"];
        _logger.LogDebug("version is {version},bot relationId is {id},request param is {param}", version,
            _chatBotBasicInfoOptions.RelationId,input.Keywords);
        if (!string.IsNullOrEmpty(platform) && !string.IsNullOrEmpty(version))
        {
            var curVersion = new Version(version.ToString().Replace("v", ""));
            var preVersion = new Version(_chatBotBasicInfoOptions.Version.Replace("v", ""));
            if (platform != "extension" && curVersion >= preVersion)
            {
                return result.Where(t => !ChatConstant.ChatDisplayName.Equals(t.Name)).ToList();
            }
        }

        return result.Where(t => !ChatConstant.ChatDisplayName.Equals(t.Name)).ToList();
    }

    [HttpPost("block")]
    public async Task<string> BlockUserAsync(BlockUserRequestDto input)
    {
        await _blockUserAppService.BlockUserAsync(input);
        return "success";
    }

    [HttpPost("unBlock")]
    public async Task<string> UnBlockUserAsync(UnBlockUserRequestDto input)
    {
        await _blockUserAppService.UnBlockUserAsync(input);
        return "success";
    }

    [HttpPost("isBlocked")]
    public async Task<bool> IsBlockedAsync(BlockUserRequestDto input)
    {
        await _blockUserAppService.IsBlockedAsync(input);
        return true;
    }

    [HttpGet("blockList")]
    public async Task<List<string>> BlockListAsync()
    {
        return await _blockUserAppService.BlockListAsync();
    }
}