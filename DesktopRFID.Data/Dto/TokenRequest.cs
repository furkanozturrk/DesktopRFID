using System.Text.Json.Serialization;

namespace DesktopRFID.Data.Dto;

public sealed class TokenRequest
{
    [JsonPropertyName("client_id")] public string ClientId { get; set; } = "";
    [JsonPropertyName("client_secret")] public string ClientSecret { get; set; } = "";
}
public sealed class TokenResponse
{
    [JsonPropertyName("token")] public string? Token { get; set; }
    [JsonPropertyName("accessToken")] public string? AccessToken { get; set; }
    [JsonPropertyName("refreshToken")] public string? RefreshToken { get; set; }
    [JsonPropertyName("expiresInSeconds")] public int? ExpiresInSeconds { get; set; }
    [JsonPropertyName("accessTokenExpiresAtUtc")] public DateTimeOffset? AccessTokenExpiresAtUtc { get; set; }
    [JsonPropertyName("refreshTokenExpiresAtUtc")] public DateTimeOffset? RefreshTokenExpiresAtUtc { get; set; }
}
public sealed class RefreshRequest
{
    [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
}