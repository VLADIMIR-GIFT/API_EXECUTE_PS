public class ProcedureRequest
{
    public string ProcedureName { get; set; }
    public required Dictionary<string, object> Parameters { get; set; }
    public EnumTypeRecordset RecordsetType { get; set; }
    public bool EnableRecordsetType { get; set; } // Nouvelle propriété booléenne
}