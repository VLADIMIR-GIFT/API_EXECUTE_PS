using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Text.Json;
using System.Linq;
using System.IO;

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
        string? databaseAlias = null; // CORRECTION CS8600
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
                        databaseAlias = EnumTypeDatabase.SqlServer.ToString();
                    }
                    else
                    {
                        throw new Exception($"Procédure ID {procedureId} trouvée dans la base de données mais impossible de récupérer ses informations après initialisation.");
                    }
                }
                else
                {
                    throw new Exception($"Procédure ID {procedureId} non trouvée dans le registre ni dans la base de données.");
                }
            }
            else
            {
                procedureName = procedureInfo.Name;
                databaseAlias = EnumTypeDatabase.SqlServer.ToString();
            }
        }
        else
        {
            databaseAlias = _configService.GetDefaultDatabaseAlias();
            if (string.IsNullOrEmpty(databaseAlias))
            {
                throw new Exception("Aucun alias de base de données par défaut n'est configuré ou le nom de la procédure n'est pas un ID valide.");
            }
        }

        var databaseConfig = _configService.GetDatabaseConfiguration(databaseAlias);
        if (databaseConfig == null)
        {
            throw new Exception($"Configuration de la base de données introuvable pour l'alias '{databaseAlias}'.");
        }

        // CORRECTION CS1061 : Caster explicitement la connexion vers SqlConnection
        using (var connection = (SqlConnection)DatabaseFactory.CreateConnection(EnumTypeDatabase.SqlServer, databaseConfig.ConnectionString))
        {
            await connection.OpenAsync(); // Maintenant OpenAsync est accessible
            using (var command = (SqlCommand)connection.CreateCommand())
            {
                command.CommandText = procedureName;
                command.CommandType = CommandType.StoredProcedure;

                try
                {
                    SqlCommandBuilder.DeriveParameters(command);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur lors de l'appel à DeriveParameters pour la procédure '{procedureName}': {ex.Message}");
                    throw new InvalidOperationException($"Impossible de dériver les paramètres pour la procédure '{procedureName}'. Vérifiez les permissions ou si la procédure existe.", ex);
                }

                foreach (SqlParameter param in command.Parameters)
                {
                    if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                    {
                        string paramName = param.ParameterName.Replace("@", "");

                        if (parameters.ContainsKey(paramName))
                        {
                            object? value = parameters[paramName]; // Utilisation de object?
                            if (value is JsonElement jsonElement)
                            {
                                value = ConvertJsonElement(jsonElement, param.SqlDbType);
                            }
                            param.Value = value ?? DBNull.Value;
                        }
                        else if (param.IsNullable)
                        {
                            param.Value = DBNull.Value;
                        }
                        else
                        {
                            throw new ArgumentException($"Le paramètre '{param.ParameterName}' est manquant dans les données JSON et n'est pas nullable.");
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

    // CORRECTION CS0103 : Implémentation correcte de ConvertJsonElement
    private object? ConvertJsonElement(JsonElement jsonElement, SqlDbType sqlDbType)
    {
        // Gère les valeurs nulles ou indéfinies de JsonElement
        if (jsonElement.ValueKind == JsonValueKind.Null || jsonElement.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        return sqlDbType switch
        {
            SqlDbType.Int => jsonElement.ValueKind == JsonValueKind.Number ? (object?)jsonElement.GetInt32() : null,
            SqlDbType.Decimal => jsonElement.ValueKind == JsonValueKind.Number ? (object?)jsonElement.GetDecimal() : null,
            SqlDbType.Bit => jsonElement.ValueKind == JsonValueKind.True || jsonElement.ValueKind == JsonValueKind.False ? (object?)jsonElement.GetBoolean() : null,
            SqlDbType.DateTime => jsonElement.ValueKind == JsonValueKind.String && DateTime.TryParse(jsonElement.GetString(), out var dt) ? (object?)dt : null,
            SqlDbType.UniqueIdentifier => jsonElement.ValueKind == JsonValueKind.String && Guid.TryParse(jsonElement.GetString(), out var guid) ? (object?)guid : null,
            // Pour les chaînes et autres types qui peuvent être convertis à partir de chaînes
            _ => jsonElement.ValueKind == JsonValueKind.String ? (object?)jsonElement.GetString() : jsonElement.ToString()
        };
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
            dataTable.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row[col.ColumnName] is DBNull ? null : row[col])
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
        var ds = new DataSet();
        using (var adapter = new SqlDataAdapter(command))
        {
            await Task.Run(() => adapter.Fill(ds));
        }
        return ds.Tables.Cast<DataTable>().Select(table => new
        {
            TableName = table.TableName,
            Headers = table.Columns.Cast<DataColumn>().Select(col => col.ColumnName).ToList(),
            Data = table.Rows.Cast<DataRow>().Select(row =>
                table.Columns.Cast<DataColumn>().ToDictionary(col => col.ColumnName, col => row[col.ColumnName] is DBNull ? null : row[col])
            ).ToList()
        }).ToList();
    }

    private async Task<object> ExecuteJsonAsync(SqlCommand command)
    {
        var dataTable = new DataTable();
        using (var adapter = new SqlDataAdapter(command))
        {
            await Task.Run(() => adapter.Fill(dataTable));
        }
        return JsonConvert.SerializeObject(ConvertDataTableToList(dataTable), Formatting.Indented);
    }

    private async Task<object> ExecuteXmlAsync(SqlCommand command)
    {
        var dataTable = new DataTable();
        using (var adapter = new SqlDataAdapter(command))
        {
            await Task.Run(() => adapter.Fill(dataTable));
        }
        using (var writer = new StringWriter())
        {
            dataTable.WriteXml(writer, XmlWriteMode.WriteSchema);
            return writer.ToString();
        }
    }
}
// Assurez-vous que cette énumération est définie une seule fois dans votre projet.
