using System.Data;

namespace IM.Mysql.Config;

public interface IDapperBaseContext
{
    IDbConnection OpenConnection();
}