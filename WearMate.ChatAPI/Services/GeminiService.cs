using System.Text;
using System.Text.Json;

namespace WearMate.ChatAPI.Services;

public class GeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
    {
        _apiKey = configuration["GEMINI_API_KEY"]
            ?? throw new Exception("GEMINI_API_KEY not found");
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://generativelanguage.googleapis.com/")
        };
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string userMessage, string conversationContext = "")
    {
        try
        {
            var systemPrompt = @"You are a helpful customer service assistant for WearMate, an online fashion store. 
Your role is to:
- Answer questions about products, orders, and shipping
- Help customers find the right products
- Provide friendly and professional support
- If you don't know something, politely ask the customer to contact human staff

Keep responses concise and helpful. Always be polite and professional.";

            var fullPrompt = $"{systemPrompt}\n\nConversation context: {conversationContext}\n\nUser: {userMessage}\n\nAssistant:";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = fullPrompt }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"v1beta/models/gemini-pro:generateContent?key={_apiKey}",
                content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini API error: {StatusCode}", response.StatusCode);
                return "I'm having trouble processing your request right now. Please try again later.";
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

            return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                ?? "I couldn't generate a response. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return "I'm experiencing technical difficulties. Please contact our support team.";
        }
    }
}

public class GeminiResponse
{
    public List<Candidate>? Candidates { get; set; }
}

public class Candidate
{
    public Content? Content { get; set; }
}

public class Content
{
    public List<Part>? Parts { get; set; }
}

public class Part
{
    public string? Text { get; set; }
}