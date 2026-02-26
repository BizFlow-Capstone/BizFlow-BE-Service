namespace BizFlow.Application.DTOs.ImportSchema
{
    public class ImportSchemaListItemDto
    {
        public int ImportSchemaId { get; set; }
        public string TemplateCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
