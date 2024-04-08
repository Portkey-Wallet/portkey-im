using System.Data;
using IM.Options;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Volo.Abp.DependencyInjection;

namespace IM.Dapper;

public class DapperContext : IDapperContext, ISingletonDependency
{
    private readonly ImDbOptions _options;

    public DapperContext(IOptionsSnapshot<ImDbOptions> options)
    {
        _options = options.Value;
    }

    public IDbConnection OpenConnection()
    {
        var dbConnection = new MySqlConnection(_options.ConnectionStrings);
        dbConnection.Open();
        return dbConnection;
    }
}