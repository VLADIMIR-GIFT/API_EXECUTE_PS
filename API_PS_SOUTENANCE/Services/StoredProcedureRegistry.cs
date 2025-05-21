using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using System.IO;

public class StoredProcedureRegistry
{
    private readonly Dictionary<int, StoredProcedureInfo> _procedures = new Dictionary<int, StoredProcedureInfo>();
    private readonly string _connectionString;
    private readonly string _databaseName;
    private readonly XmlConfigurationService _configService;

    public StoredProcedureRegistry(XmlConfigurationService configService)
    {
        _configService = configService;
        string defaultDatabaseAlias = _configService.GetDefaultDatabaseAlias();
        if (string.IsNullOrEmpty(defaultDatabaseAlias))
        {
            throw new Exception("L'alias de base de données par défaut n'a pas été trouvé dans la configuration XML.");
        }

        _connectionString = _configService.GetConnectionString(defaultDatabaseAlias);
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new Exception($"La chaîne de connexion pour '{defaultDatabaseAlias}' n'a pas été trouvée.");
        }

        _databaseName = _configService.GetDatabaseNameFromConnectionString(_connectionString);
        if (string.IsNullOrEmpty(_databaseName))
        {
            throw new ArgumentException("Impossible d'extraire le nom de la base de données pour StoredProcedureRegistry.");
        }
    }

    public async Task InitializeProcedures()
    {
        using (var connection = SqlConnectionFactorys.CreateConnection(_connectionString))
        {
            await connection.OpenAsync();
            var query = $@"SELECT routine_name
                             FROM information_schema.routines
                             WHERE routine_type = 'PROCEDURE'
                             AND routine_catalog = '{_databaseName}'
                             AND routine_schema = 'dbo'";

            using (var command = new SqlCommand(query, connection))
            using (var reader = await command.ExecuteReaderAsync())
            {
                int id = 1000;
                _procedures.Clear();
                while (await reader.ReadAsync())
                {
                    _procedures.Add(id, new StoredProcedureInfo
                    {
                        Id = id,
                        Name = reader.GetString(0),
                        DatabaseType = EnumTypeDatabase.SqlServer
                    });
                    id--;
                }
            }
        }

        var procedureList = GetProcedureList();
        try
        {
            File.WriteAllText("ProcedureList.json", JsonConvert.SerializeObject(procedureList, Formatting.Indented));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'écriture de ProcedureList.json: {ex.Message}");
        }
    }

    public StoredProcedureInfo? GetProcedureInfo(int procedureId) // Peut retourner null
    {
        if (_procedures.TryGetValue(procedureId, out var procedureInfo))
        {
            return procedureInfo;
        }
        return null;
    }

    public Dictionary<int, string> GetProcedureList()
    {
        var procedureList = new Dictionary<int, string>();
        foreach (var procedure in _procedures)
        {
            procedureList.Add(procedure.Key, procedure.Value.Name);
        }
        return procedureList;
    }

    public async Task<bool> CheckProcedureExistsInDatabase(int procedureId)
    {
        if (!_procedures.ContainsKey(procedureId))
        {
            await InitializeProcedures();
            if (!_procedures.ContainsKey(procedureId))
            {
                return false;
            }
        }

        using (var connection = SqlConnectionFactorys.CreateConnection(_connectionString))
        {
            await connection.OpenAsync();
            var query = $@"SELECT COUNT(*)
                             FROM information_schema.routines
                             WHERE routine_type = 'PROCEDURE'
                             AND routine_catalog = '{_databaseName}'
                             AND routine_schema = 'dbo'
                             AND routine_name = @ProcedureName";

            using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ProcedureName", _procedures[procedureId].Name);

                // CORRECTION CS8605 ici
                object? result = await command.ExecuteScalarAsync(); // Permet un retour null
                int count = (result == null || result == DBNull.Value) ? 0 : Convert.ToInt32(result);
                return count > 0;
            }
        }
    }

    public Dictionary<int, StoredProcedureInfo> GetProcedures()
    {
        return _procedures;
    }
}
