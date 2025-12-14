
namespace DesktopRFID.Data.Services;

public static class TokenStore
{
    public static string? AccessToken { get; private set; }
    public static string? RefreshToken { get; private set; }
    public static DateTimeOffset? AccessExpiresAtUtc { get; private set; }
    public static DateTimeOffset? RefreshExpiresAtUtc { get; private set; }
    public static void Set(string accessToken, int? expiresInSeconds, string? refreshToken = null,
                           DateTimeOffset? accessExpAtUtc = null, DateTimeOffset? refreshExpAtUtc = null)
    {
        AccessToken = accessToken;
        RefreshToken = refreshToken ?? RefreshToken;
        AccessExpiresAtUtc = accessExpAtUtc ?? (expiresInSeconds is > 0
            ? DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds.Value)
            : null);
        RefreshExpiresAtUtc = refreshExpAtUtc ?? RefreshExpiresAtUtc;
    }
    public static bool HasValidAccessToken(TimeSpan? skew = null)
    {
        var s = skew ?? TimeSpan.FromSeconds(30);
        return !string.IsNullOrWhiteSpace(AccessToken) &&
               (AccessExpiresAtUtc is null || AccessExpiresAtUtc > DateTimeOffset.UtcNow.Add(s));
    }
    public static bool CanRefresh =>
        !string.IsNullOrWhiteSpace(RefreshToken) &&
        (RefreshExpiresAtUtc is null || RefreshExpiresAtUtc > DateTimeOffset.UtcNow.AddSeconds(30));
    public static void Clear()
    {
        AccessToken = null; RefreshToken = null;
        AccessExpiresAtUtc = null; RefreshExpiresAtUtc = null;
    }
}