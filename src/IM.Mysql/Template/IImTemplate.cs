using System.Data;

namespace IM.Mysql.Template;

public interface IImTemplate<T> where T : class
{
    Task<T> QueryFirstOrDefaultAsync(string sql, object param = null);

    Task<IEnumerable<T>> QueryAsync(string sql, object param = null, IDbTransaction transaction = null,
        int? commandTimeout = null, CommandType? commandType = null);
}

public interface IImTemplate
{
    Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null);

    Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null,
        int? commandTimeout = null, CommandType? commandType = null);
    
    Task<(IEnumerable<T> data, int totalCount)> QueryPageAsync<T>(string sql, object param = null,
        IDbTransaction transaction = null,
        int? commandTimeout = null, CommandType? commandType = null);
    
    Task ExecuteAsync(string sql, object param = null);
}