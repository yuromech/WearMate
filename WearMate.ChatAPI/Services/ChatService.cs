using WearMate.ChatAPI.Data;
using WearMate.Shared.DTOs.Chat;

namespace WearMate.ChatAPI.Services;

public class ChatService
{
    private readonly SupabaseClient _supabase;
    private readonly GeminiService _gemini;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        SupabaseClient supabase,
        GeminiService gemini,
        ILogger<ChatService> logger)
    {
        _supabase = supabase;
        _gemini = gemini;
        _logger = logger;
    }

    public async Task<List<ConversationDto>> GetConversationsAsync(Guid? userId = null, string? status = null)
    {
        var query = _supabase.From("chat_conversations")
            .OrderBy("updated_at", false);

        if (userId.HasValue)
            query.Eq("user_id", userId.Value);

        if (!string.IsNullOrEmpty(status))
            query.Eq("status", status);

        return await _supabase.GetAsync<ConversationDto>(query.Build());
    }

    public async Task<ConversationDto?> GetConversationByIdAsync(Guid id)
    {
        var query = _supabase.From("chat_conversations").Eq("id", id);
        return await _supabase.GetSingleAsync<ConversationDto>(query.Build());
    }

    public async Task<List<MessageDto>> GetMessagesAsync(Guid conversationId, int limit = 50)
    {
        var query = _supabase.From("chat_messages")
            .Eq("conversation_id", conversationId)
            .OrderBy("created_at", true)
            .Limit(limit);

        return await _supabase.GetAsync<MessageDto>(query.Build());
    }

    public async Task<MessageDto?> SendMessageAsync(SendMessageDto dto)
    {
        ConversationDto? conversation;

        if (dto.ConversationId.HasValue)
        {
            conversation = await GetConversationByIdAsync(dto.ConversationId.Value);
        }
        else
        {
            var convData = new
            {
                user_id = dto.UserId,
                status = "active",
                is_ai_active = true,
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };
            conversation = await _supabase.PostAsync<ConversationDto>("chat_conversations", convData);
        }

        if (conversation == null)
            throw new Exception("Conversation not found or failed to create");

        var userMessageData = new
        {
            conversation_id = conversation.Id,
            sender_type = "customer",
            sender_id = dto.UserId,
            message = dto.Message,
            created_at = DateTime.UtcNow
        };

        var userMessage = await _supabase.PostAsync<MessageDto>("chat_messages", userMessageData);

        if (conversation.IsAiActive && conversation.AssignedStaffId == null)
        {
            var conversationHistory = await GetMessagesAsync(conversation.Id);
            var context = string.Join("\n", conversationHistory.TakeLast(5)
                .Select(m => $"{m.SenderType}: {m.Message}"));

            var aiResponse = await _gemini.GenerateResponseAsync(dto.Message, context);

            var aiMessageData = new
            {
                conversation_id = conversation.Id,
                sender_type = "ai",
                message = aiResponse,
                created_at = DateTime.UtcNow
            };

            await _supabase.PostAsync<MessageDto>("chat_messages", aiMessageData);
        }

        var updateConvData = new { updated_at = DateTime.UtcNow };
        await _supabase.PatchAsync<ConversationDto>("chat_conversations", conversation.Id, updateConvData);

        return userMessage;
    }

    public async Task<bool> StaffTakeoverAsync(StaffTakeoverDto dto)
    {
        var conversation = await GetConversationByIdAsync(dto.ConversationId);
        if (conversation == null) return false;

        var data = new
        {
            assigned_staff_id = dto.StaffId,
            is_ai_active = false,
            updated_at = DateTime.UtcNow
        };

        var result = await _supabase.PatchAsync<ConversationDto>(
            "chat_conversations", dto.ConversationId, data);

        if (result != null)
        {
            var systemMessageData = new
            {
                conversation_id = dto.ConversationId,
                sender_type = "staff",
                sender_id = dto.StaffId,
                message = "A staff member has joined the conversation.",
                created_at = DateTime.UtcNow
            };
            await _supabase.PostAsync<MessageDto>("chat_messages", systemMessageData);
        }

        return result != null;
    }

    public async Task<bool> CloseConversationAsync(CloseConversationDto dto)
    {
        var data = new
        {
            status = "closed",
            updated_at = DateTime.UtcNow
        };

        var result = await _supabase.PatchAsync<ConversationDto>(
            "chat_conversations", dto.ConversationId, data);

        return result != null;
    }

    public async Task<MessageDto?> SendStaffMessageAsync(Guid conversationId, Guid staffId, string message)
    {
        var messageData = new
        {
            conversation_id = conversationId,
            sender_type = "staff",
            sender_id = staffId,
            message = message,
            created_at = DateTime.UtcNow
        };

        var result = await _supabase.PostAsync<MessageDto>("chat_messages", messageData);

        var updateData = new { updated_at = DateTime.UtcNow };
        await _supabase.PatchAsync<ConversationDto>("chat_conversations", conversationId, updateData);

        return result;
    }
}