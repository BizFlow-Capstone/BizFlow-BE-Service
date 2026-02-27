namespace BizFlow.Application.DTOs.ImportSchema
{
    public class UpdateImportSchemaRequest
    {
        /// <summary>
        /// Update template code (optional)
        /// </summary>
        public string? TemplateCode { get; set; }

        /// <summary>
        /// Update template name (optional)
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// If provided and different from current active version → creates new version
        /// </summary>
        public string? SchemaJson { get; set; }
    }
}
