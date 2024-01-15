using System.Threading.Tasks;
using IM.Image;
using IM.Image.Dto;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace IM.Controllers;



[RemoteService]
[Area("app")]
[ControllerName("Image")]
[Route("api/v1/image")]
public class ImageController : ImController
{
    private readonly IImageAppService _imageAppService;

    public ImageController(IImageAppService imageAppService)
    {
        _imageAppService = imageAppService;
    }
    
    
    [HttpGet("getThumbnail")]
    public async Task<string> GetThumbnailAsync(GetThumbnailInput input)
    {
        return await _imageAppService.GetThumbnailAsync(input);
    }
   
    
    
    
    
    
    
}