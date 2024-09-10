using System.Linq;
using System.Threading.Tasks;
using IM.Contact;
using IM.Contact.Dtos;
using IM.RelationOne.Dtos.Contact;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace IM.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Contact")]
[Route("api/v1/contacts")]
[Authorize]
[IgnoreAntiforgeryToken]
public class ContactController : ImController
{
    private readonly IContactAppService _contactAppService;
    

    public ContactController(IContactAppService contactAppService)
    {
        _contactAppService = contactAppService;
    }

    [HttpGet("profile")]
    public async Task<ContactInfoDto> GetContactProfileAsync(ContactProfileRequestDto input)
    {
        return await _contactAppService.GetContactProfileAsync(input);
    }

    [HttpPost("follow")]
    public async Task<string> FollowAsync(FollowsRequestDto input)
    {
        await _contactAppService.FollowAsync(input);
        return "success";
    }

    [HttpPost("unfollow")]
    public async Task<string> UnFollowAsync(FollowsRequestDto input)
    {
        await _contactAppService.UnFollowAsync(input);
        return "success";
    }

    [HttpGet("list")]
    public async Task<PagedResultDto<ContactProfileDto>> GetListAsync(ContactGetListRequestDto input)
    {
        var result = await _contactAppService.GetListAsync(input);
        if (!result.Items.IsNullOrEmpty())
        {
            result.Items = result.Items.Where(contract => !"KeyGenie".Equals(contract.Name)).ToList();
        }
        return result;
    }

    [HttpPost("stranger")]
    public async Task<AddStrangerResultDto> AddStrangerAsync(AddStrangerDto input)
    {
        return await _contactAppService.AddStrangerAsync(input);
    }
    
    [HttpPost("remark")]
    public async Task<string> RemarkAsync(RemarkRequestDto input)
    {
        await _contactAppService.RemarkAsync(input);
        return "success";
    }
}