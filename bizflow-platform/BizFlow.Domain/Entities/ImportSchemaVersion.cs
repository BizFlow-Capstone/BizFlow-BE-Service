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
    /// JSON mapping definition for data transformation
    /// </summary>
    public string? MappingJson { get; set; }

    /// <summary>
    /// URL of the template file for this version
    /// </summary>
    public string? TemplateFileUrl { get; set; }

    /// <summary>
    /// Human-readable version label
    /// </summary>
    public string? VersionLabel { get; set; }

    /// <summary>
    /// Only one active version per schema at a time
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// When this version becomes effective
    /// </summary>
    public DateTime? EffectiveFrom { get; set; }

    /// <summary>
    /// When this version was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// FK to Profiles - who created this version
    /// </summary>
    public Guid? CreatedBy { get; set; }

    public virtual Profile? CreatedByNavigation { get; set; }

    public virtual ImportSchema ImportSchema { get; set; } = null!;

    public virtual ICollection<Import> Imports { get; set; } = new List<Import>();
}
