using System.Threading.Tasks;
using IM.Transfer;
using IM.Transfer.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace IM.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Transfer")]
[Route("api/v1/transfer")]
[Authorize]
public class TransferController : ImController
{
    private readonly ITransferAppService _transferAppService;
    public TransferController(ITransferAppService transferAppService)
    {
        _transferAppService = transferAppService;
    }

    [HttpPost("send")]
    public async Task<TransferOutputDto> SendTransferAsync(TransferInputDto input)
    {
        return await _transferAppService.SendTransferAsync(input);
    }
    
    [HttpGet("getResult")]
    public async Task<TransferResultDto> GetResultAsync(string transferId)
    {
        return await _transferAppService.GetResultAsync(transferId);
    }
}