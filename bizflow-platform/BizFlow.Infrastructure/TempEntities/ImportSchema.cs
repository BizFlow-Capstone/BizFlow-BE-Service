using System;
using System.Collections.Generic;

namespace BizFlow.Infrastructure.TempEntities;

public partial class ImportSchemas
{
    public int ImportSchemaId { get; set; }

    /// <summary>
    /// Unique code identifying the template type
    /// </summary>
    public string TemplateCode { get; set; } = null!;

    /// <summary>
    /// Human-readable name of the template
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Whether this schema template is available for use
    /// </summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// True if this schema has ever been set as active (gates soft vs hard delete)
    /// </summary>
    public bool EverActivated { get; set; }

    /// <summary>
    /// When this schema was first created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Soft delete timestamp; NULL means not deleted
    /// </summary>
    public DateTime? DeletedAt { get; set; }

    public virtual ICollection<ImportSchemaVersions> ImportSchemaVersions { get; set; } = new List<ImportSchemaVersions>();
}
