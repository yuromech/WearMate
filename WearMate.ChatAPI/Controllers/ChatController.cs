using Microsoft.AspNetCore.Mvc;
using WearMate.ChatAPI.Services;
using WearMate.Shared.DTOs.Chat;
using WearMate.Shared.DTOs.Common;

namespace WearMate.ChatAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly ChatService _chatService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(ChatService chatService, ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    [HttpGet("conversations")]
    public async Task<ActionResult<ApiResponse<List<ConversationDto>>>> GetConversations(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? status = null)
    {
        try
        {
            var conversations = await _chatService.GetConversationsAsync(userId, status);
            return Ok(ApiResponse<List<ConversationDto>>.SuccessResponse(conversations));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations");
            return StatusCode(500, ApiResponse<List<ConversationDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("conversations/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> GetConversation(Guid id)
    {
        try
        {
            var conversation = await _chatService.GetConversationByIdAsync(id);
            if (conversation == null)
                return NotFound(ApiResponse<ConversationDto>.ErrorResponse("Conversation not found"));
            return Ok(ApiResponse<ConversationDto>.SuccessResponse(conversation));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation");
            return StatusCode(500, ApiResponse<ConversationDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("messages/{conversationId:guid}")]
    public async Task<ActionResult<ApiResponse<List<MessageDto>>>> GetMessages(
        Guid conversationId,
        [FromQuery] int limit = 50)
    {
        try
        {
            var messages = await _chatService.GetMessagesAsync(conversationId, limit);
            return Ok(ApiResponse<List<MessageDto>>.SuccessResponse(messages));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages");
            return StatusCode(500, ApiResponse<List<MessageDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("send")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendMessage([FromBody] SendMessageDto dto)
    {
        try
        {
            var message = await _chatService.SendMessageAsync(dto);
            return Ok(ApiResponse<MessageDto>.SuccessResponse(message, "Message sent"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, ApiResponse<MessageDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("staff-takeover")]
    public async Task<ActionResult<ApiResponse<bool>>> StaffTakeover([FromBody] StaffTakeoverDto dto)
    {
        try
        {
            var result = await _chatService.StaffTakeoverAsync(dto);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Conversation not found"));
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Staff takeover successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in staff takeover");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("close")]
    public async Task<ActionResult<ApiResponse<bool>>> CloseConversation([FromBody] CloseConversationDto dto)
    {
        try
        {
            var result = await _chatService.CloseConversationAsync(dto);
            if (!result)
                return NotFound(ApiResponse<bool>.ErrorResponse("Conversation not found"));
            return Ok(ApiResponse<bool>.SuccessResponse(true, "Conversation closed"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing conversation");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("staff-message")]
    public async Task<ActionResult<ApiResponse<MessageDto>>> SendStaffMessage(
        [FromBody] StaffMessageRequest request)
    {
        try
        {
            var message = await _chatService.SendStaffMessageAsync(
                request.ConversationId, request.StaffId, request.Message);

            if (message == null)
                return StatusCode(500, ApiResponse<MessageDto>.ErrorResponse("Failed to send message"));

            return Ok(ApiResponse<MessageDto>.SuccessResponse(message, "Message sent"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending staff message");
            return StatusCode(500, ApiResponse<MessageDto>.ErrorResponse(ex.Message));
        }
    }
}

public class StaffMessageRequest
{
    public Guid ConversationId { get; set; }
    public Guid StaffId { get; set; }
    public string Message { get; set; } = string.Empty;
}