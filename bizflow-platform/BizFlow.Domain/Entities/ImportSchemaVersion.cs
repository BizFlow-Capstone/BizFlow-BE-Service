using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class ImportSchemaVersion
{
    public int ImportSchemaVersionId { get; set; }

    /// <summary>
    /// Reference to parent ImportSchema
    /// </summary>
    public int ImportSchemaId { get; set; }

    /// <summary>
    /// JSON schema definition for the import template
    /// </summary>
    public string SchemaJson { get; set; } = null!;

    /// <summary>
    /// Only one active version per schema at a time
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When this version was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    public virtual ImportSchema ImportSchema { get; set; } = null!;
}
