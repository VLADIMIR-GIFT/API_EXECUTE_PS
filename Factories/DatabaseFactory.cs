using System.Data;
using System.Data.SqlClient; // Pour SQL Server
using MySql.Data.MySqlClient; // Pour MySQL
using Oracle.ManagedDataAccess.Client; // Pour Oracle
using Microsoft.Data.Sqlite; // Pour SQLite
using Microsoft.Data.SqlClient;
using Npgsql; // Pour PostgreSQL
using System.Data.OleDb;//pour Access
public class DatabaseFactory
{
    public static IDbConnection CreateConnection(EnumTypeDatabase typeDatabase, string connectionString)
    {
        return typeDatabase switch
        {
            EnumTypeDatabase.base_ecole => new SqlConnection(connectionString),
            EnumTypeDatabase.BddMySql => new MySqlConnection(connectionString),
            EnumTypeDatabase.BddPostGreSql => new NpgsqlConnection(connectionString),
            EnumTypeDatabase.BddOracle => new OracleConnection(connectionString),
            EnumTypeDatabase.BddAccess => new OleDbConnection(connectionString),
            EnumTypeDatabase.BddSqlLite3 => new SqliteConnection(connectionString),
            _ => throw new NotImplementedException("Type de base non supporté.")
        };
    }
}