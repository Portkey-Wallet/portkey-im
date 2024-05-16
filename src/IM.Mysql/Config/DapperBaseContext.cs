using System.Data;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Volo.Abp.DependencyInjection;

namespace IM.Mysql.Config;

public class DapperBaseContext : IDapperBaseContext, ISingletonDependency
{
    private readonly ImBaseDbOptions _options;

    public DapperBaseContext(IOptionsSnapshot<ImBaseDbOptions> options)
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