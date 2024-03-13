using System;
using System.Data;

namespace IM.Dapper;

public interface IDapperContext : IDisposable
{
    IDbConnection OpenConnection();
}