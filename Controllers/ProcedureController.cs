using Microsoft.AspNetCore.Mvc;
using API_PS_SOUTENANCE.Models;
using API_PS_SOUTENANCE.Services;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using System.Data;
using System;

namespace API_PS_SOUTENANCE.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProcedureController : ControllerBase
    {
        private readonly ProcedureService _procedureService;

        public ProcedureController(ProcedureService procedureService)
        {
            _procedureService = procedureService;
        }

        [HttpPost("executer_procedure")]
        public async Task<IActionResult> ExecuterProcedure([FromBody] JsonElement jsonData)
        {
            try
            {
                string rawJson = jsonData.GetRawText();
                var request = JsonSerializer.Deserialize<ProcedureRequest>(rawJson);

                if (request == null)
                {
                    return BadRequest("Les données de la requête sont invalides.");
                }

                var dbParameters = await _procedureService.GetProcedureParametersAsync(request.ProcedureName);
                var sqlParameters = new List<SqlParameter>();

                foreach (var paramDef in dbParameters)
                {
                    if (request.Params.ContainsKey(paramDef.ParameterName.Replace("@", "")))
                    {
                        var value = request.Params[paramDef.ParameterName.Replace("@", "")];
                        value = HandleJsonTypes(value, paramDef.SqlDbType);
                        sqlParameters.Add(new SqlParameter(paramDef.ParameterName, value ?? DBNull.Value));
                    }
                    else if (paramDef.ParameterName == "@TraceErrorLigne")
                    {
                        // On ne fait rien ici pour @TraceErrorLigne pour l'instant
                    }
                    else
                    {
                        return BadRequest($"Le paramètre '{paramDef.ParameterName}' est manquant dans les données JSON.");
                    }
                }

                // *** Correction cruciale : Créer TraceErrorLigne AVANT le premier appel à ExecuteProcedureAsync ***
                var traceErrorParam = new SqlParameter("@TraceErrorLigne", SqlDbType.VarChar, 8000);
                traceErrorParam.Direction = ParameterDirection.Output;
                sqlParameters.Add(traceErrorParam); // Ajout à la collection dès le début


                // Exécution de la procédure (le paramètre @TraceErrorLigne est maintenant inclus)
                await _procedureService.ExecuteProcedureAsync(request.ProcedureName, sqlParameters);


                // Récupération du paramètre @TraceErrorLigne (plus besoin de recréer les paramètres)
                string traceError = traceErrorParam.Value?.ToString(); // On récupère directement la valeur du paramètre

                if (!string.IsNullOrEmpty(traceError))
                {
                    return BadRequest($"Erreur lors de l'exécution de la procédure : {traceError}");
                }

                return Ok("Procédure exécutée avec succès");
            }
            catch (JsonException ex)
            {
                return BadRequest($"Erreur de désérialisation JSON: {ex.Message}");
            }
            catch (Exception ex)
            {
                return BadRequest($"Erreur: {ex.Message}");
            }
        }

        private object HandleJsonTypes(object value, SqlDbType sqlDbType)
        {
            if (value == null)
                return DBNull.Value;

            if (value.GetType().Name == "JsonElement")
            {
                JsonElement jsonElement = (JsonElement)value;

                switch (jsonElement.ValueKind)
                {
                    case JsonValueKind.Number:
                        if (sqlDbType == SqlDbType.Int) return jsonElement.GetInt32();
                        if (sqlDbType == SqlDbType.Decimal) return jsonElement.GetDecimal();
                        if (sqlDbType == SqlDbType.BigInt) return jsonElement.GetInt64();
                        if (sqlDbType == SqlDbType.Float) return jsonElement.GetDouble();
                        break;
                    case JsonValueKind.String:
                        return jsonElement.GetString();
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return jsonElement.GetBoolean();
                    case JsonValueKind.Null:
                        return DBNull.Value;
                    case JsonValueKind.Object:
                    case JsonValueKind.Array:
                        return jsonElement.ToString();
                    default:
                        throw new JsonException($"Type de données JSON non pris en charge: {jsonElement.ValueKind}");
                }
            }
            return value;
        }
    }
}