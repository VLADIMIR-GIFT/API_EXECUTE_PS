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
        private readonly string _databaseName; // Nouvelle variable pour le nom de la base de données
        private readonly XmlConfigurationService _configService; // Nouvelle injection de service

        // MODIFIER LE CONSTRUCTEUR
        public ProcedureService(string connectionString, XmlConfigurationService configService)
        {
            _connectionString = connectionString;
            _configService = configService;
            // Extrait le nom de la base de données de la chaîne de connexion
            _databaseName = _configService.GetDatabaseNameFromConnectionString(connectionString);

            if (string.IsNullOrEmpty(_databaseName))
            {
                throw new ArgumentException("Impossible d'extraire le nom de la base de données de la chaîne de connexion.", nameof(connectionString));
            }
        }

        public string ConnectionString => _connectionString;

        // MODIFIER CETTE MÉTHODE
        public async Task<List<string>> GetStoredProceduresAsync()
        {
            var procedures = new List<string>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Remplacer 'GRIM_EMECEF_2025' par la variable dynamique _databaseName
                var query = $@"SELECT routine_name
                                 FROM information_schema.routines
                                 WHERE routine_type = 'PROCEDURE'
                                 AND routine_catalog = '{_databaseName}'
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

        // MODIFIER CETTE MÉTHODE
        public async Task<List<SqlParameter>> GetProcedureParametersAsync(string procedureName)
        {
            var parameters = new List<SqlParameter>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Remplacer 'GRIM_EMECEF_2025' par la variable dynamique _databaseName
                var query = $@"SELECT parameter_name, data_type
                                 FROM information_schema.parameters
                                 WHERE specific_name = @ProcedureName
                                 AND specific_catalog = '{_databaseName}'
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
                            SqlDbType = GetSqlDbType(reader.GetString(1))
                        };
                        parameters.Add(param);
                    }
                }
            }

            return parameters;
        }

        // Le reste de la classe ProcedureService reste inchangé
        public async Task ExecuteProcedureAsync(string procedureName, Dictionary<string, object> inputParams)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    SqlCommandBuilder.DeriveParameters(command);

                    foreach (SqlParameter param in command.Parameters)
                    {
                        if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                        {
                            string paramName = param.ParameterName.Replace("@", "");
                            if (inputParams.ContainsKey(paramName))
                            {
                                object value = inputParams[paramName];
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
                                throw new ArgumentException($"Le paramètre '{paramName}' est manquant dans la requête.");
                            }
                        }
                    }
                    await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<Dictionary<string, object>>> ExecuteProcedureWithResultsAsync(string procedureName, Dictionary<string, object> inputParams)
        {
            var results = new List<Dictionary<string, object>>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(procedureName, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    SqlCommandBuilder.DeriveParameters(command);
                    foreach (SqlParameter param in command.Parameters)
                    {
                        if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                        {
                            string paramName = param.ParameterName.Replace("@", "");
                            if (inputParams.ContainsKey(paramName))
                            {
                                object value = inputParams[paramName];
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
                _ => SqlDbType.NVarChar,
            };
        }
    }
}