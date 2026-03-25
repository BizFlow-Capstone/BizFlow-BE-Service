using System;
using System.Collections.Generic;

namespace BizFlow.Domain.Entities;

public partial class NotificationTemplate
{
    public Guid NotificationTemplateId { get; set; }

    public string EventCode { get; set; } = null!;

    public string NotificationType { get; set; } = null!;

    public string TitleTemplate { get; set; } = null!;

    public string ContentTemplate { get; set; } = null!;

    public string? DefaultActionType { get; set; }

    public string? DefaultTargetScreen { get; set; }

    public string? DefaultActionPayloadJson { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<NotificationDispatch> NotificationDispatches { get; set; } = new List<NotificationDispatch>();
}