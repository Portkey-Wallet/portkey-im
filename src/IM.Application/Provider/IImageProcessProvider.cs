using System.Threading.Tasks;

namespace IM.Provider;

public interface IImageProcessProvider
{
    Task<string> GetResizeImageAsync(string imageUrl, int width, int height);
}