namespace ApexProp.Infrastructure.Services;

public class AnthropicResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("content")]
    public List<AnthropicContent>? Content { get; set; }
}

public class AnthropicContent
{
    [System.Text.Json.Serialization.JsonPropertyName("text")]
    public string? Text { get; set; }
}