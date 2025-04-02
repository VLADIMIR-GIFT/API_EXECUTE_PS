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
                              AND routine_catalog = 'GRIM_EMECEF_2025'
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
                              AND specific_catalog = 'GRIM_EMECEF_2025'
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
                            SqlDbType = GetSqlDbType(reader.GetString(1)) // Convertir le type de données SQL
                        };
                        parameters.Add(param);
                    }
                }
            }

            return parameters;
        }

        // Exécuter une procédure stockée avec des paramètres
        public async Task ExecuteProcedureAsync(string procedureName, Dictionary<string, object> inputParams)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Utilisation de DeriveParameters pour récupérer les paramètres exacts
                    SqlCommandBuilder.DeriveParameters(command);

                    foreach (SqlParameter param in command.Parameters)
                    {
                        if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                        {
                            string paramName = param.ParameterName.Replace("@", ""); // Supprimer @ du nom du paramètre JSON
                            
                            if (inputParams.ContainsKey(paramName))
                            {
                                object value = inputParams[paramName];

                                // Conversion sécurisée des types pour éviter les erreurs
                                if (param.SqlDbType == SqlDbType.VarChar || param.SqlDbType == SqlDbType.NVarChar)
                                {
                                    param.Value = value?.ToString(); // Forcer la conversion en string
                                }
                                else if (param.SqlDbType == SqlDbType.Int)
                                {
                                    param.Value = Convert.ToInt32(value);
                                }
                                else if (param.SqlDbType == SqlDbType.Decimal)
                                {
                                    param.Value = Convert.ToDecimal(value);
                                }
                                else if (param.SqlDbType == SqlDbType.DateTime)
                                {
                                    param.Value = Convert.ToDateTime(value);
                                }
                                else
                                {
                                    param.Value = value ?? DBNull.Value;
                                }
                            }
                            else
                            {
                                throw new ArgumentException($"Le paramètre '{paramName}' est manquant dans la requête.");
                            }
                        }
                    }

                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        // Exécution de la procédure et récupération des résultats
        public async Task<List<Dictionary<string, object>>> ExecuteProcedureWithResultsAsync(string procedureName, Dictionary<string, object> inputParams)
        {
            var results = new List<Dictionary<string, object>>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Utilisation de DeriveParameters
                    SqlCommandBuilder.DeriveParameters(command);

                    foreach (SqlParameter param in command.Parameters)
                    {
                        if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                        {
                            string paramName = param.ParameterName.Replace("@", "");

                            if (inputParams.ContainsKey(paramName))
                            {
                                object value = inputParams[paramName];

                                // Sécurisation des conversions
                                if (param.SqlDbType == SqlDbType.VarChar || param.SqlDbType == SqlDbType.NVarChar)
                                {
                                    param.Value = value?.ToString();
                                }
                                else if (param.SqlDbType == SqlDbType.Int)
                                {
                                    param.Value = Convert.ToInt32(value);
                                }
                                else if (param.SqlDbType == SqlDbType.Decimal)
                                {
                                    param.Value = Convert.ToDecimal(value);
                                }
                                else if (param.SqlDbType == SqlDbType.DateTime)
                                {
                                    param.Value = Convert.ToDateTime(value);
                                }
                                else
                                {
                                    param.Value = value ?? DBNull.Value;
                                }
                            }
                            else
                            {
                                throw new ArgumentException($"Le paramètre '{paramName}' est manquant.");
                            }
                        }
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
            }

            return results;
        }

        // Méthode pour déterminer le type SQL approprié pour un paramètre
        private SqlDbType GetSqlDbType(string dataType)
        {
            return dataType.ToLower() switch
            {
                "varchar" or "text" => SqlDbType.VarChar,
                "int" => SqlDbType.Int,
                "date" => SqlDbType.Date,
                "bit" => SqlDbType.Bit,
                "datetime" => SqlDbType.DateTime,
                "nvarchar" => SqlDbType.NVarChar,
                "uniqueidentifier" => SqlDbType.UniqueIdentifier,
                "decimal" => SqlDbType.Decimal,
                _ => SqlDbType.NVarChar, // Par défaut
            };
        }
    }
}