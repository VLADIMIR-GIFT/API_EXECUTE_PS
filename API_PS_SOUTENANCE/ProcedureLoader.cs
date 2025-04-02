using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Linq;

public class ProcedureLoader
{
    private readonly XmlConfigurationService _configService;
    private readonly Helper _helper;
    private readonly StoredProcedureRegistry _procedureRegistry;

    public ProcedureLoader(XmlConfigurationService configService, Helper helper, StoredProcedureRegistry procedureRegistry)
    {
        _configService = configService;
        _helper = helper;
        _procedureRegistry = procedureRegistry;
    }

    public async Task<object> ExecuteProcedureAsync(string procedureName, Dictionary<string, object> parameters, EnumTypeRecordset recordsetType)
    {
        string databaseAlias = null;
        if (int.TryParse(procedureName, out int procedureId))
        {
            var procedureInfo = _procedureRegistry.GetProcedureInfo(procedureId);
            if (procedureInfo == null)
            {
                if (await _procedureRegistry.CheckProcedureExistsInDatabase(procedureId))
                {
                    await _procedureRegistry.InitializeProcedures();
                    procedureInfo = _procedureRegistry.GetProcedureInfo(procedureId);
                    if (procedureInfo != null)
                    {
                        procedureName = procedureInfo.Name;
                        databaseAlias = procedureInfo.DatabaseType.ToString();
                    }
                    else
                    {
                        throw new Exception($"Procedure ID {procedureId} non trouvé dans la base de données.");
                    }
                }
                else
                {
                    throw new Exception($"Procedure ID {procedureId} non trouvé.");
                }
            }
            else
            {
                procedureName = procedureInfo.Name;
                databaseAlias = procedureInfo.DatabaseType.ToString();
            }
        }
        else
        {
            databaseAlias = _configService.GetDefaultDatabaseAlias();
        }

        var databaseConfig = _configService.GetDatabaseConfiguration(databaseAlias);
        if (databaseConfig == null)
        {
            throw new Exception($"Configuration de la base de données introuvable pour l'alias '{databaseAlias}'.");
        }

        using (var connection = SqlConnectionFactorys.CreateConnection(databaseConfig.ConnectionString))
        {
            await connection.OpenAsync();
            using (var command = new SqlCommand(procedureName, connection))
            {
                command.CommandType = CommandType.StoredProcedure;

                try
                {
                    SqlCommandBuilder.DeriveParameters(command);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'appel à DeriveParameters : {ex.Message}");
                }

                foreach (SqlParameter param in command.Parameters)
                {
                    if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                    {
                        string paramName = param.ParameterName.Replace("@", "");

                        if (parameters.ContainsKey(paramName))
                        {
                            object value = parameters[paramName];

                            if (value is JsonElement jsonElement)
                            {
                                value = ConvertJsonElement(jsonElement, param.SqlDbType);
                            }

                            param.Value = value ?? DBNull.Value;
                        }
                        else
                        {
                            throw new ArgumentException($"Le paramètre {param.ParameterName} est manquant dans les données JSON.");
                        }
                    }
                }

                switch (recordsetType)
                {
                    case EnumTypeRecordset.DataTable:
                        return await ExecuteDataTableAsync(command);
                    case EnumTypeRecordset.DataSet:
                        return await ExecuteDataSetAsync(command);
                    case EnumTypeRecordset.DataJson:
                        return await ExecuteJsonAsync(command);
                    case EnumTypeRecordset.DataXml:
                        return await ExecuteXmlAsync(command);
                    default:
                        throw new ArgumentException("Type de recordset non pris en charge.");
                }
            }
        }
    }

    private async Task<object> ExecuteDataTableAsync(SqlCommand command)
    {
        var dataTable = new DataTable();
        using (var adapter = new SqlDataAdapter(command))
        {
            await Task.Run(() => adapter.Fill(dataTable));
        }

        List<string> headers;
        if (dataTable.Columns.Count > 0)
        {
            headers = dataTable.Columns.Cast<DataColumn>().Select(col => col.ColumnName).ToList();
        }
        else
        {
            headers = new List<string>();
        }

        var data = dataTable.Rows.Cast<DataRow>().Select(row =>
            dataTable.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row[col.ColumnName])
        ).ToList();

        return new { Headers = headers, Data = data };
    }

    private List<Dictionary<string, object>> ConvertDataTableToList(DataTable dataTable)
    {
        var list = new List<Dictionary<string, object>>();

        foreach (DataRow row in dataTable.Rows)
        {
            var dict = new Dictionary<string, object>();
            foreach (DataColumn col in dataTable.Columns)
            {
                dict[col.ColumnName] = row[col] is DBNull ? null : row[col];
            }
            list.Add(dict);
        }

        return list;
    }

    private async Task<object> ExecuteDataSetAsync(SqlCommand command)
    {
        using (var adapter = new SqlDataAdapter(command))
        {
            var dataSet = new DataSet();
            await Task.Run(() => adapter.Fill(dataSet));

            var results = dataSet.Tables.Cast<DataTable>().Select(table => new
            {
                Headers = table.Columns.Cast<DataColumn>().Select(col => col.ColumnName).ToList(),
                Data = table.Rows.Cast<DataRow>().Select(row =>
                    table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row[col.ColumnName])
                ).ToList()
            }).ToList();

            return results;
        }
    }

    private async Task<string> ExecuteJsonAsync(SqlCommand command)
    {
        var dataSet = await ExecuteDataSetAsync(command);
        return JsonConvert.SerializeObject(dataSet);
    }

    private async Task<string> ExecuteXmlAsync(SqlCommand command)
    {
        var dataSet = await ExecuteDataSetAsync(command);
        return System.Text.Json.JsonSerializer.Serialize(dataSet);
    }

    private object ConvertJsonElement(JsonElement element, SqlDbType dbType)
    {
        try
        {
            return dbType switch
            {
                SqlDbType.Int => element.GetInt32(),
                SqlDbType.BigInt => element.GetInt64(),
                SqlDbType.Decimal => element.GetDecimal(),
                SqlDbType.Float => element.GetDouble(),
                SqlDbType.Bit => element.GetBoolean(),
                SqlDbType.NVarChar or SqlDbType.VarChar or SqlDbType.Text => element.GetString(),
                SqlDbType.DateTime or SqlDbType.Date => element.GetDateTime(),
                _ => element.ToString()
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erreur de conversion du paramètre {element} vers {dbType}: {ex.Message}", ex);
        }
    }
}