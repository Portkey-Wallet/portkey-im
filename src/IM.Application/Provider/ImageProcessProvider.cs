using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using IM.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace IM.Provider;

public class ImageProcessProvider : IImageProcessProvider, ISingletonDependency
{
    private readonly ILogger<ImageProcessProvider> _logger;
    private HttpClient? Client { get; set; }
    private readonly AWSThumbnailOptions _awsThumbnailOptions;
    public ImageProcessProvider(ILogger<ImageProcessProvider> logger, IOptions<AWSThumbnailOptions> awsThumbnailOptions)
    {
        _logger = logger;
        _awsThumbnailOptions = awsThumbnailOptions.Value;
    }

    public async Task<string> GetResizeImageAsync(string imageUrl, int width, int height)
    {
        try
        {
            if (!imageUrl.Contains(ImageConstant.AwsDomain))
            {
                return imageUrl;
            }
            var produceImage = GetResizeUrl(imageUrl, width, height, true);
            await SendUrlAsync(produceImage);

            var resImage = GetResizeUrl(imageUrl, width, height, false);
            return resImage;
        }
        catch (Exception ex)
        {
            _logger.LogError("sendImageRequest Execption:", ex);
            return imageUrl;
        }
    }

    private string GetResizeUrl(string imageUrl, int width, int height, bool replaceDomain)
    {
        if (replaceDomain)
        {
            var urlSplit = imageUrl.Split(new string[] { ImageConstant.AwsDomain },
                StringSplitOptions.RemoveEmptyEntries);
            imageUrl = _awsThumbnailOptions.BaseUrl + urlSplit[1];
        }

        var lastIndexOf = imageUrl.LastIndexOf("/", StringComparison.Ordinal);
        var pre = imageUrl.Substring(0, lastIndexOf);
        var last = imageUrl.Substring(lastIndexOf, imageUrl.Length - lastIndexOf);
        var resizeImage = pre + "/" + (width == -1 ? "AUTO" : width) + "x" + (height == -1 ? "AUTO" : height) + last;
        return resizeImage;
    }

    private async Task SendUrlAsync(string url, string? version = null)
    {
        Client ??= new HttpClient();
        Client.DefaultRequestHeaders.Accept.Clear();
        Client.DefaultRequestHeaders.Accept.Add(
            MediaTypeWithQualityHeaderValue.Parse($"application/json{version}"));
        Client.DefaultRequestHeaders.Add("Connection", "close");
        await Client.GetAsync(url);
    }
}