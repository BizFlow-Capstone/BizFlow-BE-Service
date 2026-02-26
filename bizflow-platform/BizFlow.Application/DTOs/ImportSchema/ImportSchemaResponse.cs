namespace BizFlow.Application.DTOs.ImportSchema
{
    public class ImportSchemaResponse : ImportSchemaListItemDto
    {
        public bool EverActivated { get; set; }

        /// <summary>
        /// JSON schema from the current active version (null if no active version)
        /// </summary>
        public string? SchemaJson { get; set; }
    }
}
