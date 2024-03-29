using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace IM.Dapper.Repository;

public interface IImRepository<T> where T : class
{
    Task<T> QueryFirstOrDefaultAsync(string sql, object param = null);

    Task<IEnumerable<T>> QueryAsync(string sql, object param = null, IDbTransaction transaction = null,
        int? commandTimeout = null, CommandType? commandType = null);
}

public interface IImRepository
{
    Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null);

    Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null,
        int? commandTimeout = null, CommandType? commandType = null);
    
    Task<(IEnumerable<T> data, int totalCount)> QueryPageAsync<T>(string sql, object param = null,
        IDbTransaction transaction = null,
        int? commandTimeout = null, CommandType? commandType = null);
}