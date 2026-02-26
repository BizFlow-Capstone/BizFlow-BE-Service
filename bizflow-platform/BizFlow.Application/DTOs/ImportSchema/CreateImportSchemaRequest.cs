namespace BizFlow.Application.DTOs.ImportSchema
{
    public class CreateImportSchemaRequest
    {
        /// <summary>
        /// Unique code identifying the template type
        /// </summary>
        public string TemplateCode { get; set; } = null!;

        /// <summary>
        /// Human-readable name of the template
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// JSON schema definition (required on create — first version)
        /// </summary>
        public string SchemaJson { get; set; } = null!;
    }
}
