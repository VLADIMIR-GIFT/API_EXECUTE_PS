using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

[ApiController]
[Route("api/[controller]")]
public class ProcedureController : ControllerBase
{
    private readonly ProcedureLoader _procedureLoader;
    private readonly JsonSerializerSettings _jsonSerializerSettings;

    public ProcedureController(ProcedureLoader procedureLoader)
    {
        _procedureLoader = procedureLoader;

        // Configuration de la sérialisation avec Newtonsoft.Json
        _jsonSerializerSettings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore, // Ignore les références circulaires
            Formatting = Formatting.Indented, // Format JSON lisible
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter(), // Convertit les enums en string
                new TypeJsonConverter()    // Convertisseur pour System.Type
            }
        };
    }

    [HttpPost("execute")]
    public async Task<IActionResult> ExecuteProcedure([FromBody] ProcedureRequest request)
    {
        try
        {
            // Exécuter la procédure stockée
            var result = await _procedureLoader.ExecuteProcedureAsync(
                request.DatabaseAlias,
                request.ProcedureName,
                request.Parameters,
                request.RecordsetType
            );

            // Sérialisation du résultat en JSON
            var jsonResponse = JsonConvert.SerializeObject(result, _jsonSerializerSettings);

            return Ok(jsonResponse);
        }
        catch (Exception ex)
        {
            // Capture et journalisation de l'erreur
            TraceErrorLine(ex);
            return StatusCode(500, "Une erreur interne s'est produite. Consultez le fichier JournalError.txt pour plus de détails.");
        }
    }

    /// <summary>
    /// Capture l'erreur et l'enregistre dans un fichier "JournalError.txt".
    /// </summary>
    /// <param name="ex">Exception capturée.</param>
    private void TraceErrorLine(Exception ex)
    {
        string filePath = Path.Combine(Directory.GetCurrentDirectory(), "JournalError.txt");

        try
        {
            string errorMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Erreur : {ex.Message}\n" +
                                  $"StackTrace : {ex.StackTrace}\n" +
                                  $"------------------------------------------------------------\n";

            // Écriture dans le fichier journal
            System.IO.File.AppendAllText(filePath, errorMessage);
        }
        catch (Exception logEx)
        {
            Console.WriteLine($"Erreur lors de l'écriture du journal : {logEx.Message}");
        }
    }
}

/// <summary>
/// Convertisseur personnalisé pour System.Type afin d'éviter les erreurs de sérialisation.
/// </summary>
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
            writer.WriteValue(type.AssemblyQualifiedName); // Convertit en nom de type
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