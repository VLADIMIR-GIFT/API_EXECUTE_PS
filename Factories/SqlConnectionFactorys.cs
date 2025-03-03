
using Microsoft.Data.SqlClient;

public class SqlConnectionFactorys
{
    public static SqlConnection CreateConnection(string connectionString)
    {
        return new SqlConnection(connectionString);
    }
}