using System.Threading.Tasks;
using IM.Image.Dto;

namespace IM.Image;

public interface IImageAppService
{
    
    Task<string> GetThumbnailAsync(GetThumbnailInput input);
}