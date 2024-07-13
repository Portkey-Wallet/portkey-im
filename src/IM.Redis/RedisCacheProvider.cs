using IM.Cache;
using StackExchange.Redis;
using Volo.Abp.DependencyInjection;

namespace IM.Redis;

public class RedisCacheProvider : ICacheProvider, ISingletonDependency
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IDatabase _database;

    public RedisCacheProvider(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _database = _connectionMultiplexer.GetDatabase();
    }

    public async Task HSetWithExpire(string key, string member, string value, TimeSpan? expire)
    {
        _database.HashSet(key, member, value);
        _database.KeyExpire(key, expire);
    }

    public async Task<bool> HashDeleteAsync(string key, string member)
    {
        return _database.HashDelete(key, member);
    }

    public async Task<HashEntry[]> HGetAll(string key)
    {
        return await _database.HashGetAllAsync(key);
    }

    public async Task Set(string key, string value, TimeSpan? expire)
    {
        await _database.StringSetAsync(key, value);
        _database.KeyExpire(key, expire);
    }

    public async Task<RedisValue> Get(string key)
    {
        return await _database.StringGetAsync(key);
    }

    public async Task Delete(string key)
    {
        _database.KeyDelete(key);
    }

    public async Task<Dictionary<string, RedisValue>> BatchGet(List<string> keys)
    {
        var batch = _database.CreateBatch();
        var tmpAns = new Dictionary<string, Task<RedisValue>>(keys.Count);
        foreach (var key in keys)
        {
            tmpAns[key] = batch.StringGetAsync(key);
        }

        batch.Execute();
        var realAns = new Dictionary<string, RedisValue>(keys.Count);
        foreach (var kv in tmpAns.Where(kv => kv.Value != null))
        {
            realAns[kv.Key] = kv.Value.Result;
        }

        return realAns;
    }

    public async Task<long> Increase(string key, int increase, TimeSpan? expire)
    {
        var count = await _database.StringIncrementAsync(key, increase);
        if (expire != null)
        {
            _database.KeyExpire(key, expire);
        }

        return count;
    }
    
    
    public async Task AddScoreAsync(string leaderboardKey, string member, double score)
    {
        await _database.SortedSetAddAsync(leaderboardKey, member, score);
    }

    public async Task<double> GetScoreAsync(string leaderboardKey, string member)
    {
        return await _database.SortedSetScoreAsync(leaderboardKey, member) ?? 0;
    }

    public async Task<long> GetRankAsync(string leaderboardKey, string member, bool highToLow = true)
    {
        long? rank;

        if (highToLow)
        {
            rank = await _database.SortedSetRankAsync(leaderboardKey, member, Order.Descending);
        }
        else
        {
            rank = await _database.SortedSetRankAsync(leaderboardKey, member);
        }

        return rank ?? -1; // -1 indicates that the member is not in the leaderboard
    }

    public async Task<SortedSetEntry[]> GetTopAsync(string leaderboardKey, long startRank, long stopRank, bool highToLow = true)
    {
        var order = highToLow ? Order.Descending : Order.Ascending;
        return await _database.SortedSetRangeByRankWithScoresAsync(leaderboardKey, startRank, stopRank, order);
    }

    public async Task<long> GetSortedSetLengthAsync(string leaderboardKey)
    {
        var length = await _database.SortedSetLengthAsync(leaderboardKey);
        return length;
    }
    
    
}