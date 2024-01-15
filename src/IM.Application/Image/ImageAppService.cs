using System.Threading.Tasks;
using IM.Image.Dto;
using IM.Provider;

namespace IM.Image;

public class ImageAppService : ImAppService, IImageAppService
{
    
    private readonly IImageProcessProvider _imageProcessProvider;

    public ImageAppService(IImageProcessProvider imageProcessProvider)
    {
        _imageProcessProvider = imageProcessProvider;
    }


    public async Task<string> GetThumbnailAsync(GetThumbnailInput input)
    {
        return await _imageProcessProvider.GetResizeImageAsync(input.ImageUrl, input.Width, input.Height);
    }
}