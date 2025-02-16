
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace API_PS_SOUTENANCE.Models
{
    public class ProcedureRequest
    {
        public string ProcedureName { get; set; } = string.Empty;
        public Dictionary<string, object> Params { get; set; } = new Dictionary<string, object>();

        // Méthode pour désérialiser les params sous forme de JsonElement
        public void SetParamsFromJson(JsonElement jsonElement)
        {
            foreach (var item in jsonElement.EnumerateObject())
            {
                Params[item.Name] = item.Value;
            }
        }
    }
}