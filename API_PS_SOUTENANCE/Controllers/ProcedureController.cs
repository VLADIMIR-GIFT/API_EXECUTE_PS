using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

[ApiController]
public class ProcedureController : ControllerBase
{
    private readonly ProcedureLoader _procedureLoader;
    private readonly JsonSerializerSettings _jsonSerializerSettings;
    private readonly StoredProcedureRegistry _procedureRegistry;

    public ProcedureController(ProcedureLoader procedureLoader, StoredProcedureRegistry procedureRegistry)
    {
        _procedureLoader = procedureLoader;
        _procedureRegistry = procedureRegistry;

        _jsonSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Formatting = Formatting.Indented,
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter(),
                new TypeJsonConverter()
            }
        };
    }

    [HttpPost("executeProcedure")]
    public async Task<IActionResult> ExecuteProcedure([FromBody] ProcedureRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.ProcedureName))
            {
                return BadRequest("Veuillez entrer le numéro correspondant à la procédure stockée que vous souhaitez exécuter.");
            }

            object result = null; // Initialisation de result

            if (request.EnableRecordsetType)
            {
                result = await _procedureLoader.ExecuteProcedureAsync(
                    request.ProcedureName,
                    request.Parameters,
                    request.RecordsetType
                );
            }
            else
            {
                // Si EnableRecordsetType est false, on execute la procedure sans prendre en compte le recordsetType.
                await _procedureLoader.ExecuteProcedureAsync(
                    request.ProcedureName,
                    request.Parameters,
                    EnumTypeRecordset.DataTable // Ce paramètre n'est pas utilisé ici
                );
            }

            // Retourner un message de succès
            var successMessage = new { Message = "Procédure exécutée avec succès" };

            // Ajouter le résultat si present
            if (result != null)
            {
                var jsonResult = JsonConvert.SerializeObject(result, _jsonSerializerSettings);
                return Ok(new { successMessage, Result = JsonConvert.DeserializeObject(jsonResult) });
            }
            else
            {
                return Ok(successMessage);
            }

        }
        catch (Exception ex)
        {
            TraceErrorLine(ex);
            return StatusCode(500, "Une erreur interne s'est produite. Consultez le fichier JournalError.txt pour plus de détails.");
        }
    }

    private void TraceErrorLine(Exception ex)
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "JournalError.txt");

        try
        {
            string errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Erreur : {ex.Message}\n" +
                                    $"StackTrace : {ex.StackTrace}\n" +
                                    $"------------------------------------------------------------\n";

            System.IO.File.AppendAllText(filePath, errorMessage);
        }
        catch (Exception logEx)
        {
            Console.WriteLine($"Erreur lors de l'écriture du journal : {logEx.Message}");
        }
    }

    public class TypeJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Type);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is Type type)
            {
                writer.WriteValue(type.AssemblyQualifiedName);
            }
            else
            {
                writer.WriteNull();
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            string typeName = reader.Value as string;
            return typeName != null ? Type.GetType(typeName) : null;
        }
    }
}