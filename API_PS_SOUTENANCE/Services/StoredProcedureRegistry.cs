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

    public StoredProcedureRegistry(XmlConfigurationService configService)
    {
        _connectionString = configService.GetConnectionString("GRIM_EMECEF_2025");
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new Exception("Chaîne de connexion pour 'GRIM_EMECEF_2025' non trouvée.");
        }

        InitializeProcedures().Wait();
    }

    public async Task InitializeProcedures()
    {
        using (var connection = SqlConnectionFactorys.CreateConnection(_connectionString))
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
                int id = 1000;
                _procedures.Clear();
                while (await reader.ReadAsync())
                {
                    _procedures.Add(id, new StoredProcedureInfo
                    {
                        Id = id,
                        Name = reader.GetString(0),
                        DatabaseType = EnumTypeDatabase.GRIM_EMECEF_2025
                    });
                    id--;
                }
            }
        }

        var procedureList = GetProcedureList();
        System.IO.File.WriteAllText("ProcedureList.json", JsonConvert.SerializeObject(procedureList, Formatting.Indented));
    }

    public StoredProcedureInfo GetProcedureInfo(int procedureId)
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
        using (var connection = SqlConnectionFactorys.CreateConnection(_connectionString))
        {
            await connection.OpenAsync();
            var query = @"SELECT COUNT(*)
                          FROM information_schema.routines
                          WHERE routine_type = 'PROCEDURE'
                          AND routine_catalog = 'GRIM_EMECEF_2025'
                          AND routine_schema = 'dbo'
                          AND routine_name = @ProcedureName";

            var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ProcedureName", _procedures[procedureId].Name);

            int count = (int)await command.ExecuteScalarAsync();
            return count > 0;
        }
    }

    public Dictionary<int, StoredProcedureInfo> GetProcedures()
    {
        return _procedures;
    }
}

