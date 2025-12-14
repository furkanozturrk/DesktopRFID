using DesktopRFID.Data.Interfaces;
using System.Net;
using System.Net.Http.Json;

namespace DesktopRFID.Data.Services;

public sealed class ApiClient : IDisposable
{
    private readonly HttpClient _http;
    private IAuthService? _auth;
    private readonly IFileLogger _logger;
    public ApiClient(IFileLogger logger, string baseUrl, IAuthService? auth = null)
    {
        _logger = logger;
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(30) };
        _auth = auth;
    }
    public void AttachAuthService(IAuthService auth) => _auth = auth;
    public async Task<TResp?> PostJsonAsync<TReq, TResp>(string path, TReq body, bool withAuth = false)
    {
        if (withAuth) await EnsureAuthAsync();

        using var resp = await SendWithAuthRetry(() => _http.PostAsJsonAsync(path, body), withAuth);
        if (!resp.IsSuccessStatusCode)
            await ThrowApiException(resp);

        return await resp.Content.ReadFromJsonAsync<TResp>();
    }
    public async Task<TResp?> GetJsonAsync<TResp>(string path, bool withAuth = false)
    {
        if (withAuth) await EnsureAuthAsync();

        using var resp = await SendWithAuthRetry(() => _http.GetAsync(path), withAuth);
        if (!resp.IsSuccessStatusCode)
            await ThrowApiException(resp);

        return await resp.Content.ReadFromJsonAsync<TResp>();
    }
    private async Task EnsureAuthAsync()
    {
        if (HasValidAccessToken())
        {
            _http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenStore.AccessToken);
            return;
        }

        if (_auth != null && TokenStore.CanRefresh)
        {
            var ok = await _auth.RefreshAsync();
            if (ok && HasValidAccessToken())
            {
                _http.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenStore.AccessToken);
                return;
            }
        }

        _http.DefaultRequestHeaders.Authorization = null;
    }
    private async Task<HttpResponseMessage> SendWithAuthRetry(Func<Task<HttpResponseMessage>> send, bool withAuth)
    {
        try
        {
            var resp = await send();

            if (withAuth && resp.StatusCode == HttpStatusCode.Unauthorized && _auth != null && TokenStore.CanRefresh)
            {
                resp.Dispose();
                var refreshed = await _auth.RefreshAsync();
                if (refreshed && HasValidAccessToken())
                {
                    _http.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", TokenStore.AccessToken);
                    resp = await send();
                }
            }

            return resp;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, " SendWithAuthRetry");
            throw;
        }

    }
    private static bool HasValidAccessToken()
    {
        try { return TokenStore.HasValidAccessToken(); } catch { }
        return !string.IsNullOrWhiteSpace(TokenStore.AccessToken);
    }
    private static async Task ThrowApiException(HttpResponseMessage resp)
    {
        var raw = await resp.Content.ReadAsStringAsync();
        string userMessage = LocalizeMessage(TryExtractMessage(raw)) ?? MapByStatusTr(resp.StatusCode);
        throw new ApiException(resp.StatusCode, userMessage, raw);
    }
    private static string? TryExtractMessage(string raw)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(raw);
            var root = doc.RootElement;

            if (root.TryGetProperty("message", out var msg) && msg.ValueKind == System.Text.Json.JsonValueKind.String)
                return msg.GetString();

            if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == System.Text.Json.JsonValueKind.String)
                return detail.GetString();

            if (root.TryGetProperty("title", out var title) && title.ValueKind == System.Text.Json.JsonValueKind.String)
                return title.GetString();
        }
        catch { }

        return null;
    }
    private static string? LocalizeMessage(string? msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return null;
        var m = msg.Trim();

        if (m.Equals("Username or password is incorrect", StringComparison.OrdinalIgnoreCase))
            return "Kullanıcı adı veya şifre hatalı.";

        if (m.Equals("invalid client credentials", StringComparison.OrdinalIgnoreCase))
            return "İstemci bilgileri (Client Id / Secret) geçersiz.";

        if (m.Contains("Authorization header", StringComparison.OrdinalIgnoreCase))
            return "Yetkilendirme başlığı (Bearer token) eksik veya geçersiz.";

        return m;
    }
    private static string MapByStatusTr(HttpStatusCode code) => code switch
    {
        HttpStatusCode.BadRequest => "İstek hatalı. Gönderdiğiniz bilgileri kontrol edin.",
        HttpStatusCode.Unauthorized => "Yetkisiz. Lütfen oturum açın veya kimlik bilgilerinizi doğrulayın.",
        HttpStatusCode.Forbidden => "Erişim izniniz yok.",
        HttpStatusCode.NotFound => "İstenilen servis bulunamadı.",
        HttpStatusCode.RequestTimeout => "Zaman aşımı. İnternet bağlantınızı kontrol edin.",
        HttpStatusCode.InternalServerError => "Sunucu hatası. Lütfen daha sonra tekrar deneyin.",
        _ => $"Beklenmeyen bir hata oluştu. (Kod {(int)code})"
    };
    public void Dispose() => _http.Dispose();
}