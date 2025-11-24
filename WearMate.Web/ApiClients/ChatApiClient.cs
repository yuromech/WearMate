using System.Text;
using System.Text.Json;
using WearMate.Shared.DTOs.Chat;
using WearMate.Shared.DTOs.Common;

namespace WearMate.Web.ApiClients;

public class ChatApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ChatApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<List<MessageDto>?> GetMessagesAsync(Guid conversationId)
    {
        var response = await _httpClient.GetAsync($"/api/chat/messages/{conversationId}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<MessageDto>>>(json, _jsonOptions);
        return apiResponse?.Data;
    }

    public async Task<MessageDto?> SendMessageAsync(SendMessageDto dto)
    {
        var json = JsonSerializer.Serialize(dto);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("/api/chat/send", content);
        if (!response.IsSuccessStatusCode) return null;

        var responseJson = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<MessageDto>>(responseJson, _jsonOptions);
        return apiResponse?.Data;
    }

    public async Task<List<ConversationDto>?> GetUserConversationsAsync(Guid userId)
    {
        var response = await _httpClient.GetAsync($"/api/chat/conversations?userId={userId}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var apiResponse = JsonSerializer.Deserialize<ApiResponse<List<ConversationDto>>>(json, _jsonOptions);
        return apiResponse?.Data;
    }
}