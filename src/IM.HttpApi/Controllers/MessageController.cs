using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Message;
using IM.Message.Dtos;
using IM.Message.Provider;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace IM.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("ImMessage")]
[Route("api/v1/message")]
public class MessageController : ImController
{
    private readonly IMessageAppService _messageAppService;
    private readonly IMessageAppProvider _messageAppProvider;

    public MessageController(IMessageAppService messageAppService, IMessageAppProvider messageAppProvider)
    {
        _messageAppService = messageAppService;
        _messageAppProvider = messageAppProvider;
    }

    [Authorize, HttpPost("send")]
    public async Task<SendMessageResponseDto> SendMessageAsync(SendMessageRequestDto input)
    {
        return await _messageAppService.SendMessageAsync(input);
    }


    [HttpPost("read")]
    public async Task<int> ReadMessageAsync(ReadMessageRequestDto input)
    {
        return await _messageAppService.ReadMessageAsync(input);
    }


    [HttpPost("hide")]
    public async Task HideMessageAsync(HideMessageRequestDto input)
    { 
        await _messageAppService.HideMessageAsync(input);
    }


    [Authorize, HttpGet("list")]
    public async Task<List<ListMessageResponseDto>> ListMessageAsync(
        ListMessageRequestDto input)
    {
        return await _messageAppService.ListMessageAsync(input);
    }


    [HttpGet("unreadCount")]
    public async Task<UnreadCountResponseDto> GetUnreadMessageCountAsync()
    {
        return await _messageAppService.GetUnreadMessageCountAsync();
    }

    [HttpPost("event")]
    public async Task EvenProcessAsync(EventMessageRequestDto input)
    {
        return;
        //await _messageAppService.EventProcessAsync(input);
    }
    
    [HttpPost("hideByLeader")]
    public async Task HideMessageByLeaderAsync(HideMessageByLeaderRequestDto input)
    { 
        await _messageAppProvider.HideMessageByLeaderAsync(input);
    }
}