using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace IM.Cache;

public interface ICacheProvider
{
    Task HSetWithExpire(string key, string member, string value, TimeSpan? expire);
    Task<bool> HashDeleteAsync(string key, string member);
    Task<HashEntry[]> HGetAll(string key);
    Task Set(string key, string value, TimeSpan? expire);
    Task<RedisValue> Get(string key);
    Task Delete(string key);
    Task<Dictionary<string, RedisValue>> BatchGet(List<string> keys);
    Task<long> Increase(string key, int increase,TimeSpan? expire);
    Task AddScoreAsync(string leaderboardKey, string member, double score);

    Task<double> GetScoreAsync(string leaderboardKey, string member);

    Task<long> GetRankAsync(string leaderboardKey, string member, bool highToLow = true);

    Task<SortedSetEntry[]> GetTopAsync(string leaderboardKey, long startRank, long stopRank, bool highToLow = true);

    Task<long> GetSortedSetLengthAsync(string leaderboardKey);
}