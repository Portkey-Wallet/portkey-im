using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Feed.Dtos;
using JetBrains.Annotations;

namespace IM.Feed;

public interface IBaseFeedAppService
{
    Task<ListFeedResponseDto> ListFeedAsync(ListFeedRequestDto input, [CanBeNull] IDictionary<string, string> headers);

    Task PinFeedAsync(PinFeedRequestDto input);

    Task MuteFeedAsync(MuteFeedRequestDto input);

    Task HideFeedAsync(HideFeedRequestDto input);
}