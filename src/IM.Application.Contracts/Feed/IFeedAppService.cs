using System.Collections.Generic;
using System.Threading.Tasks;
using IM.Feed.Dtos;
using IM.Feed.Etos;

namespace IM.Feed;

public interface IFeedAppService : IBaseFeedAppService
{
    Task<FeedMetaDto> GetFeedMetaAsync(string id);

    Task<bool> FeedMetaSetRunningAsync(string id);

    Task<bool> FeedMetaSetIdleAsync(string id);

    Task ProcessFeedAsync(EventFeedListEto eventData, string id);

    Task DeleteByRelationIdAsync(string relationId);

    Task DeleteByChannelIdAsync(string channelId);

    Task<List<FeedInfoDto>> GetByRelationIdListAsync(string relationId);
}