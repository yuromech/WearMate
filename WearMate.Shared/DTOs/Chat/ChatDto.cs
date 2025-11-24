namespace WearMate.Shared.DTOs.Chat;

public class ConversationDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? AssignedStaffId { get; set; }
    public string Status { get; set; } = "active"; // active, closed
    public bool IsAiActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Last message info
    public string? LastMessage { get; set; }
    public DateTime? LastMessageAt { get; set; }

    // User info
    public string UserName { get; set; } = string.Empty;
    public string? StaffName { get; set; }
}

public class MessageDto
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public string SenderType { get; set; } = string.Empty; // customer, ai, staff
    public Guid? SenderId { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Sender info
    public string SenderName { get; set; } = string.Empty;
}

// Operation DTOs
public class SendMessageDto
{
    public Guid? ConversationId { get; set; } // null = new conversation
    public Guid UserId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class StaffTakeoverDto
{
    public Guid ConversationId { get; set; }
    public Guid StaffId { get; set; }
}

public class CloseConversationDto
{
    public Guid ConversationId { get; set; }
}