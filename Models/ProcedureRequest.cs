using System.Collections.Generic;

public class ProcedureRequest
{
    public required string DatabaseAlias { get; set; }
    public required string ProcedureName { get; set; }
    public required Dictionary<string, object> Parameters { get; set; } // Renommé pour éviter le conflit
    public EnumTypeRecordset RecordsetType { get; set; } 
}

