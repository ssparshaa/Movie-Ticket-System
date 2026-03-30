using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;

public class OracleDbService
{
    private readonly string _connectionString;

    public OracleDbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("OracleDbConnection")
                            ?? throw new InvalidOperationException("Connection string not found.");
    }

    public OracleConnection GetConnection()
    {
        var conn = new OracleConnection(_connectionString);
        conn.Open();
        return conn;
    }
}