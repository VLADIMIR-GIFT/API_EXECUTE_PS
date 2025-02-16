
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

namespace API_PS_SOUTENANCE.Services
{
    public class ProcedureService
    {
        private readonly string _connectionString;

        // Constructeur pour initialiser la chaîne de connexion
        public ProcedureService(string connectionString)
        {
            _connectionString = connectionString;
            
        }
        public string ConnectionString => _connectionString;

        // Récupérer toutes les procédures stockées dans la base "base_ecole"
        public async Task<List<string>> GetStoredProceduresAsync()
        {
            var procedures = new List<string>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"SELECT routine_name
                              FROM information_schema.routines
                              WHERE routine_type = 'PROCEDURE' 
                              AND routine_catalog = 'base_ecole'
                              AND routine_schema = 'dbo'";

                var command = new SqlCommand(query, connection);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        procedures.Add(reader.GetString(0));
                    }
                }
            }

            return procedures;
        }

        // Récupérer les paramètres d'une procédure stockée donnée
        public async Task<List<SqlParameter>> GetProcedureParametersAsync(string procedureName)
        {
            var parameters = new List<SqlParameter>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var query = @"SELECT parameter_name, data_type
                              FROM information_schema.parameters
                              WHERE specific_name = @ProcedureName
                              AND specific_catalog = 'base_ecole'
                              AND specific_schema = 'dbo'";

                var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@ProcedureName", procedureName);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var param = new SqlParameter
                        {
                            ParameterName = reader.GetString(0),
                            SqlDbType = GetSqlDbType(reader.GetString(1)) // Convertir le type de données
                        };
                        parameters.Add(param);
                    }
                }
            }

            return parameters;
        }

        // Exécuter une procédure stockée avec des paramètres
        public async Task ExecuteProcedureAsync(string procedureName, List<SqlParameter> parameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Ajouter tous les paramètres
                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }

                await command.ExecuteNonQueryAsync();  // Exécuter la procédure
            }
        }

        // Méthode pour déterminer le type SQL approprié pour un paramètre
        private SqlDbType GetSqlDbType(string dataType)
        {
            switch (dataType.ToLower())
            {
                case "varchar":
                case "text":
                    return SqlDbType.VarChar;
                case "int":
                    return SqlDbType.Int;
                case "date":
                    return SqlDbType.Date;
                case "bit":
                    return SqlDbType.Bit;
                case "datetime":
                    return SqlDbType.DateTime;
                case "nvarchar":
                    return SqlDbType.NVarChar;
                case "uniqueidentifier":
                    return SqlDbType.UniqueIdentifier;
                case "decimal":
                    return SqlDbType.Decimal;
                default:
                    return SqlDbType.NVarChar; // Par défaut, considérer comme NVarChar
            }
        }

        // Exécution de la procédure avec des résultats (si nécessaire)
        public async Task<List<Dictionary<string, object>>> ExecuteProcedureWithResultsAsync(string procedureName, List<SqlParameter> parameters)
        {
            var results = new List<Dictionary<string, object>>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand(procedureName, connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                foreach (var param in parameters)
                {
                    command.Parameters.Add(param);
                }

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        var row = new Dictionary<string, object>();
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            row[reader.GetName(i)] = reader.IsDBNull(i) ? DBNull.Value : reader.GetValue(i);
                        }
                        results.Add(row);
                    }
                }
            }

            return results;
        }
    }
}

