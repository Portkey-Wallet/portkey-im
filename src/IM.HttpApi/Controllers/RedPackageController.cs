using System;
using System.ComponentModel.DataAnnotations;
using IM.RedPackage;
using IM.RedPackage.Dtos;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace IM.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("RedPackage")]
[Route("api/v1/redpackage")]
public class RedPackageController : ImController
{
    private readonly IRedPackageAppService _redPackageAppService;
    
    public RedPackageController(IRedPackageAppService redPackageAppService)
    {
        _redPackageAppService = redPackageAppService;
    }
    
    [HttpPost("generate")]
    [Authorize]
    public async Task<GenerateRedPackageOutputDto> GenerateRedPackage(GenerateRedPackageInputDto redPackageInput)
    {
        return await _redPackageAppService.GenerateRedPackageAsync(redPackageInput);
    }
    
    [HttpPost("send")]
    [Authorize]
    public async Task<SendRedPackageOutputDto> SendRedPackage(SendRedPackageInputDto redPackageInput)
    {
        return await _redPackageAppService.SendRedPackageAsync(redPackageInput);
    }
    
    [HttpGet("getCreationResult")]
    [Authorize]
    public async Task<GetCreationResultOutputDto> GetCreationResult([Required] Guid sessionId)
    {
        return await _redPackageAppService.GetCreationResultAsync(sessionId);
    }
    
    [HttpGet("detail")]
    [Authorize]
    public async Task<RedPackageDetailDto> GetRedPackageDetailAsync(Guid id, int skipCount, int maxResultCount)
    {
        return await _redPackageAppService.GetRedPackageDetailAsync(id, skipCount, maxResultCount);
    }
    
    [HttpGet("config")]
    public async Task<RedPackageConfigOutput> GetRedPackageConfigAsync([CanBeNull] string chainId,
        [CanBeNull] string token)
    {
        return await _redPackageAppService.GetRedPackageConfigAsync(chainId,token);
    } 
    
    [HttpPost("grab")]
    [Authorize]
    public async Task<GrabRedPackageOutputDto> GrabRedPackageAsync(GrabRedPackageInputDto input)
    {
        return await _redPackageAppService.GrabRedPackageAsync(input);
    }
}