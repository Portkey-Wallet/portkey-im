using System.Threading.Tasks;
using IM.PinMessage;
using IM.PinMessage.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace IM.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("PinMessage")]
[Route("api/v1/pin")]
public class PinMessageController : ImController
{
    private readonly IPinMessageAppService _pinMessageAppService;

    public PinMessageController(IPinMessageAppService pinMessageAppService)
    {
        _pinMessageAppService = pinMessageAppService;
    }


    [HttpPost("list")]
    public async Task<PinMessageResponse> ListMessageAsync(
        PinMessageQueryParamDto param)
    {
        return await _pinMessageAppService.ListPinMessageAsync(param);
    }

    [HttpPost("add")]
    public async Task<PinMessageResponseDto<bool>> PinMessageAsync(
        PinMessageParamDto paramDto)
    {
        return await _pinMessageAppService.PinMessageAsync(paramDto);
    }

    [HttpPost("cancel")]
    public async Task<UnpinMessageResponseDto<bool>> UnpinMessageAsync(
        CancelPinMessageParamDto paramDto)
    {
        return await _pinMessageAppService.UnpinMessageAsync(paramDto);
    }

    [HttpPost("cancelAll")]
    public async Task<UnpinMessageResponseDto<bool>> UnpinMessageAllAsync(
        CancelPinMessageAllParamDto paramDto)
    {
        return await _pinMessageAppService.UnpinMessageAllAsync(paramDto);
    }
}