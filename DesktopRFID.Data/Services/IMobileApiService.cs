using DesktopRFID.Data.Dto;
using DesktopRFID.Data.Interfaces;
using System.Net.Http.Headers;
using System.Text.Json;

namespace DesktopRFID.Data.Services
{
    public interface IMobileApiService
    {
        Task<FilePerGarageByPlateResponse?> GetFilesPerGarageByPlateAsync(string plate, CancellationToken ct = default);
        Task<bool> AssignTagToFileAsync(int inFileId, string stRfid, string stNote, CancellationToken ct = default);
        Task<(bool ok, string? message)> DeliverByTagIdAsync(string stRfid, CancellationToken ct = default);
    }
    public sealed class MobileApiService : IMobileApiService
    {
        private readonly IAuthService? _auth;
        private readonly HttpClient _http;
        private readonly IFileLogger _logger;
        public MobileApiService(IFileLogger fileLogger, IAuthService? auth = null, HttpClient? http = null)
        {
            _auth = auth;
            _http = http ?? new HttpClient { BaseAddress = new Uri("https://test.com.tr") };
            _logger = fileLogger ?? throw new ArgumentNullException(nameof(fileLogger));
        }
        private async Task<string> GetBearerTokenAsync()
        {
            try
            {
                if (TokenStore.HasValidAccessToken()) return TokenStore.AccessToken!;
                if (_auth != null && await _auth.RefreshAsync() && TokenStore.HasValidAccessToken())
                    return TokenStore.AccessToken!;
                throw new InvalidOperationException("Oturum geçersiz veya süresi dolmuş. Lütfen yeniden giriş yapın.");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, " GetBearerTokenAsync");
                throw;
            }
        }
        private async Task PrepareHeadersAsync()
        {
            try
            {
                var token = await GetBearerTokenAsync();
                _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                if (_http.DefaultRequestHeaders.Accept.All(x => x.MediaType != "application/json"))
                    _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, " PrepareHeadersAsync");
                throw;
            }

        }
        public async Task<FilePerGarageByPlateResponse?> GetFilesPerGarageByPlateAsync(string plate, CancellationToken ct = default)
        {
            var t0 = DateTime.UtcNow;
            _logger.Info($"[GetFilesPerGarageByPlateAsync] API İstek başlıyor: plate='{plate}'");

            try
            {
                await PrepareHeadersAsync();

                using var content = new StringContent(
                    JsonSerializer.Serialize(new { plate }),
                    System.Text.Encoding.UTF8,
                    "application/json");

                using var resp = await _http.PostAsync("/v1/api/Mobile/files-per-garage-by-plate", content, ct);

                _logger.Info($"[GetFilesPerGarageByPlateAsync] HTTP {(int)resp.StatusCode} ({resp.StatusCode}), süre={(DateTime.UtcNow - t0).TotalMilliseconds:F0} ms");

                resp.EnsureSuccessStatusCode();

                var json = await resp.Content.ReadAsStringAsync(ct);

                var tr = JsonSerializer.Serialize(
                         JsonDocument.Parse(json).RootElement,
                         new JsonSerializerOptions
                         {
                             Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                         });

                var result = JsonSerializer.Deserialize<FilePerGarageByPlateResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                _logger.Info($"[GetFilesPerGarageByPlateAsync] Başarılı, Result={tr}, toplamSüre={(DateTime.UtcNow - t0).TotalMilliseconds:F0} ms");
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"[GetFilesPerGarageByPlateAsync] Hata, geçen süre={(DateTime.UtcNow - t0).TotalMilliseconds:F0} ms");
                throw;
            }
        }
        public async Task<bool> AssignTagToFileAsync(int inFileId, string stRfid, string stNote, CancellationToken ct = default)
        {
            try
            {
                _logger.Info($"Tag API Atama Başlıyor.{stNote} AssignTagToFileAsync");

                await PrepareHeadersAsync();
                var body = new { inFileId, stRfid, stNote };
                using var content = new StringContent(JsonSerializer.Serialize(body), System.Text.Encoding.UTF8, "application/json");
                using var resp = await _http.PostAsync("/v1/api/Mobile/file-rfid", content, ct);
                if (!resp.IsSuccessStatusCode) return false;

                var txt = await resp.Content.ReadAsStringAsync(ct);
                var model = JsonSerializer.Deserialize<FileRfidResponse>(txt, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _logger.Info($"Tag API Atama {stNote} ,Durum ={model?.Success} AssignTagToFileAsync");
                return model?.Success == true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, " AssignTagToFileAsync");
                throw;
            }
        }
        public async Task<(bool ok, string? message)> DeliverByTagIdAsync(string stRfid, CancellationToken ct = default)
        {
            try
            {
                _logger.Info($"Tag API Kaldırma Başlıyor.{stRfid} DeliverByTagIdAsync");

                await PrepareHeadersAsync();
                using var content = new StringContent(JsonSerializer.Serialize(new { stRfid }), System.Text.Encoding.UTF8, "application/json");
                using var resp = await _http.PostAsync("/v1/api/Mobile/rfid-deliver", content, ct);
                if (!resp.IsSuccessStatusCode)
                    return (false, $"{(int)resp.StatusCode} {resp.ReasonPhrase}");

                var txt = await resp.Content.ReadAsStringAsync(ct);
                var model = JsonSerializer.Deserialize<DeliverResponse>(txt, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                _logger.Info($"Tag API Kaldırma {stRfid} ,Durum ={model?.Status} ,Mesaj={model?.Message} DeliverByTagIdAsync");
                return (model?.Status == true, model?.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, " DeliverByTagIdAsync");
                throw;
            }
        }
        private sealed class FileRfidResponse { public bool Success { get; set; } }
        private sealed class DeliverResponse { public bool Status { get; set; } public string? Message { get; set; } }
    }
}