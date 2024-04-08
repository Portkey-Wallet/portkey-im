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

    public async Task<T> QueryFirstOrDefaultAsync(string sql, object param = null)
    {
        using var connection = _dapperContext.OpenConnection();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    public async Task<IEnumerable<T>> QueryAsync(string sql, object param = null, IDbTransaction transaction = null,
        int? commandTimeout = null, CommandType? commandType = null)
    {
        using var connection = _dapperContext.OpenConnection();
        return await connection
            .QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
    }
}

public class ImRepository : IImRepository, ISingletonDependency
{
    private readonly IDapperContext _dapperContext;

    public ImRepository(IDapperContext dapperContext)
    {
        _dapperContext = dapperContext;
    }

    public async Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null)
    {
        using var connection = _dapperContext.OpenConnection();
        return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
    }

    public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null,
        int? commandTimeout = null, CommandType? commandType = null)
    {
        using var connection = _dapperContext.OpenConnection();
        return await connection
            .QueryAsync<T>(sql, param, transaction, commandTimeout, commandType);
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