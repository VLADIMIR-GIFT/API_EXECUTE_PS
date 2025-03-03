using Microsoft.Data.SqlClient;
using System.Data;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using System.Text.Json;

public class ProcedureLoader
{
    private readonly XmlConfigurationService _configService;
    private readonly Helper _helper;

    public ProcedureLoader(XmlConfigurationService configService, Helper helper)
    {
        _configService = configService;
        _helper = helper;
    }

    public async Task<object> ExecuteProcedureAsync(string databaseAlias, string procedureName, Dictionary<string, object> parameters, EnumTypeRecordset recordsetType)
    {
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

                // Utilisation de DeriveParameters pour récupérer les paramètres attendus
                SqlCommandBuilder.DeriveParameters(command);

                // Ajout des paramètres avec conversion correcte
                foreach (SqlParameter param in command.Parameters)
                {
                    if (param.Direction == ParameterDirection.Input || param.Direction == ParameterDirection.InputOutput)
                    {
                        string paramName = param.ParameterName.Replace("@", "");

                        if (parameters.ContainsKey(paramName))
                        {
                            object value = parameters[paramName];

                            // Vérifier si la valeur est un JsonElement et convertir correctement
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

    // ✅ Nouvelle version sans DataReader - Utilisation d'un DataAdapter
    private async Task<List<Dictionary<string, object>>> ExecuteDataTableAsync(SqlCommand command)
    {
        var dataTable = new DataTable();
        using (var adapter = new SqlDataAdapter(command))
        {
            await Task.Run(() => adapter.Fill(dataTable)); // Remplit le DataTable
        }
        return ConvertDataTableToList(dataTable); // Convertit en JSON compatible Swagger
    }

    // ✅ Nouvelle méthode pour convertir DataTable en liste de dictionnaires
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

    private async Task<DataSet> ExecuteDataSetAsync(SqlCommand command)
    {
        using (var adapter = new SqlDataAdapter(command))
        {
            var dataSet = new DataSet();
            await Task.Run(() => adapter.Fill(dataSet));
            return dataSet;
        }
    }

    private async Task<string> ExecuteJsonAsync(SqlCommand command)
    {
        var dataSet = await ExecuteDataSetAsync(command);
        return JsonConvert.SerializeObject(dataSet.Tables);
    }

    private async Task<string> ExecuteXmlAsync(SqlCommand command)
    {
        var dataSet = await ExecuteDataSetAsync(command);
        return dataSet.GetXml();
    }

    // Fonction pour convertir correctement les types JSON en types SQL Server
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
                _ => element.ToString() // Par défaut, convertir en string
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Erreur de conversion du paramètre {element} vers {dbType}: {ex.Message}", ex);
        }
    }
}