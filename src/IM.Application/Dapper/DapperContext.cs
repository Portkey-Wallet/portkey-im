using System.Data;
using IM.Options;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Volo.Abp.DependencyInjection;

namespace IM.Dapper;

public class DapperContext : IDapperContext, ISingletonDependency
{
    private IDbConnection _dbConnection { get; set; }
    private readonly ImDbOptions _options;

    public DapperContext(IOptionsSnapshot<ImDbOptions> options)
    {
        _options = options.Value;
    }
    
    public IDbConnection OpenConnection()
    {
        _dbConnection ??= new MySqlConnection(_options.ConnectionStrings);
        
        if (_dbConnection.State == ConnectionState.Closed)
        {
            _dbConnection.Open();
        }
        return _dbConnection;
    }

    public void Dispose()
    {
    }
}