namespace BizFlow.Application.DTOs.Import
{
    /// <summary>
    /// Returns the JSON schema of the active ImportSchemaVersion
    /// </summary>
    public class ImportSchemaDto
    {
        public string SchemaJson { get; set; } = null!;
    }
}
