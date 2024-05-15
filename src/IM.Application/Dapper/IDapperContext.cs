using System.Data;

namespace IM.Dapper;

public interface IDapperContext
{
    IDbConnection OpenConnection();
}