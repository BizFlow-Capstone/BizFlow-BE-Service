using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class ImportSchema
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

    public virtual ICollection<ImportSchemaVersion> ImportSchemaVersions { get; set; } = new List<ImportSchemaVersion>();
}
