using SRVS.Domain.Enums;

namespace SRVS.Domain.Entities;

public class NotificationEntry : EntityBase
{
    public string RecipientUserId { get; set; } = string.Empty;

    public NotificationType Type { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? RelatedEntityType { get; set; }

    public string? RelatedEntityId { get; set; }

    public bool IsRead { get; set; }

    public DateTimeOffset? ReadAtUtc { get; set; }
}