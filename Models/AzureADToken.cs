using System.Text.Json.Serialization;

namespace PowerMauiApp.Models;

public class AzureADToken
{
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; }

    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
}
