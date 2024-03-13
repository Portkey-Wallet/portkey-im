using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Dapper;
using Volo.Abp.DependencyInjection;

namespace IM.Dapper.Repository;

public class ImRepository<T> : IImRepository<T> where T : class, ISingletonDependency
{
    private readonly IDapperContext _dapperContext;

    public ImRepository(IDapperContext dapperContext)
    {
        _dapperContext = dapperContext;
    }

    public T QueryFirstOrDefault(string sql, object param = null)
    {
        return _dapperContext.OpenConnection().QueryFirstOrDefault<T>(sql, param);
    }

    public Task<T> QueryFirstOrDefaultAsync(string sql, object param = null)
    {
        return _dapperContext.OpenConnection().QueryFirstOrDefaultAsync<T>(sql, param);
    }

    public IEnumerable<T> Query(string sql, object param = null, IDbTransaction transaction = null,
        bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
    {
        return _dapperContext.OpenConnection().Query<T>(sql, param, transaction, buffered, commandTimeout, commandType);
    }

    public Task<IEnumerable<T>> QueryAsync(string sql, object param = null, IDbTransaction transaction = null,
        int? commandTimeout = null, CommandType? commandType = null)
    {
        return _dapperContext.OpenConnection().QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }
}

public class ImRepository : IImRepository, ISingletonDependency
{
    private readonly IDapperContext _dapperContext;

    public ImRepository(IDapperContext dapperContext)
    {
        _dapperContext = dapperContext;
    }

    public T QueryFirstOrDefault<T>(string sql, object param = null)
    {
        return _dapperContext.OpenConnection().QueryFirstOrDefault<T>(sql, param);
    }

    public Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null)
    {
        return _dapperContext.OpenConnection().QueryFirstOrDefaultAsync<T>(sql, param);
    }

    public IEnumerable<T> Query<T>(string sql, object param = null, IDbTransaction transaction = null,
        bool buffered = true, int? commandTimeout = null, CommandType? commandType = null)
    {
        return _dapperContext.OpenConnection().Query<T>(sql, param, transaction, buffered, commandTimeout, commandType);
    }

    public Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null,
        int? commandTimeout = null, CommandType? commandType = null)
    {
        return _dapperContext.OpenConnection().QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }

    public async Task<(IEnumerable<T> data, int totalCount)> QueryPageAsync<T>(string sql, object param = null,
        IDbTransaction transaction = null,
        int? commandTimeout = null, CommandType? commandType = null)
    {
        await using var multi = await _dapperContext.OpenConnection()
            .QueryMultipleAsync(sql, param, transaction, commandTimeout, commandType);
        var data = await multi.ReadAsync<T>();
        var totalCount = await multi.ReadSingleAsync<int>();

        return (data, totalCount);
    }
}