using DesktopRFID.Data.Dto;
using DesktopRFID.Data.Interfaces;
using System.Security.Authentication;
using System.Text;

namespace DesktopRFID.Data.Services
{
    public interface IAuthService
    {
        Task<AuthResult> AuthenticateAsync(string clientId, string clientSecret);
        Task<bool> RefreshAsync();
    }

    public sealed class AuthService : IAuthService
    {
        private readonly ApiClient _api;
        private readonly IFileLogger Log;

        public AuthService(IFileLogger? fileLogger, ApiClient api)
        {
            Log = fileLogger;
            _api = api;
        }

        public async Task<AuthResult> AuthenticateAsync(string clientId, string clientSecret)
        {
            const string endpoint = "/v1/api/Token";
            var maskedId = MaskId(clientId);
            var started = DateTimeOffset.UtcNow;

            try
            {
                var req = new TokenRequest { ClientId = clientId, ClientSecret = clientSecret };

                var res = await _api.PostJsonAsync<TokenRequest, TokenResponse>(
                    endpoint, req, withAuth: false);

                var token = res?.AccessToken ?? res?.Token;
                if (string.IsNullOrWhiteSpace(token))
                {
                    Log.Warn($"[AUTH] RESPONSE but token is empty. endpoint='{endpoint}', clientId='{maskedId}', elapsedMs={ElapsedMs(started)}");
                    return AuthResult.Fail("Token alınamadı.");
                }

                TokenStore.Set(token!,
                               res?.ExpiresInSeconds,
                               res?.RefreshToken,
                               res?.AccessTokenExpiresAtUtc,
                               res?.RefreshTokenExpiresAtUtc);

                Log.Info($"[AUTH] SUCCESS endpoint='{endpoint}', clientId='{maskedId}', expiresIn={res?.ExpiresInSeconds}, elapsedMs={ElapsedMs(started)}");
                return AuthResult.Ok();
            }
            catch (ApiException apiEx)
            {
                Log.Error(apiEx,
                    $"[AUTH][ApiException] endpoint='{endpoint}', clientId='{maskedId}', elapsedMs={ElapsedMs(started)}\n" +
                    BuildExceptionTree(apiEx));
                return AuthResult.Fail(apiEx.UserMessage);
            }
            catch (HttpRequestException httpEx)
            {
                Log.Error(httpEx,
                    $"[AUTH][HttpRequestException] endpoint='{endpoint}', clientId='{maskedId}', httpStatus={(int?)httpEx.StatusCode} ({httpEx.StatusCode}), elapsedMs={ElapsedMs(started)}\n" +
                    BuildExceptionTree(httpEx));
                return AuthResult.Fail("Bağlantı hatası: HTTP/transport seviyesinde erişilemedi.");
            }
            catch (AuthenticationException tlsEx)
            {
                Log.Error(tlsEx,
                    $"[AUTH][TLS] Handshake/SSL hatası. endpoint='{endpoint}', clientId='{maskedId}', elapsedMs={ElapsedMs(started)}\n" +
                    BuildExceptionTree(tlsEx));
                return AuthResult.Fail("Bağlantı hatası: TLS/SSL el sıkışması başarısız.");
            }
            catch (TaskCanceledException tce)
            {
                Log.Error(tce,
                    $"[AUTH][Timeout] İstek zaman aşımına uğradı. endpoint='{endpoint}', clientId='{maskedId}', elapsedMs={ElapsedMs(started)}\n" +
                    BuildExceptionTree(tce));
                return AuthResult.Fail("Bağlantı hatası: Zaman aşımı.");
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    $"[AUTH][Unhandled] Beklenmeyen hata. endpoint='{endpoint}', clientId='{maskedId}', elapsedMs={ElapsedMs(started)}\n" +
                    BuildExceptionTree(ex));
                return AuthResult.Fail("Bağlantı hatası: " + ex.Message);
            }
            finally
            {
                Log.Info($"[AUTH] END    endpoint='{endpoint}', clientId='{maskedId}', totalMs={ElapsedMs(started)}");
            }
        }

        public async Task<bool> RefreshAsync()
        {
            const string endpoint = "/v1/api/Token/refresh";
            var started = DateTimeOffset.UtcNow;

            if (!TokenStore.CanRefresh)
            {
                Log.Warn("[AUTH] REFRESH SKIP (CanRefresh=false)");
                return false;
            }

            try
            {
                var req = new RefreshRequest { RefreshToken = TokenStore.RefreshToken };
                var res = await _api.PostJsonAsync<RefreshRequest, TokenResponse>(endpoint, req, withAuth: false);

                var newToken = res?.AccessToken ?? res?.Token;
                if (string.IsNullOrWhiteSpace(newToken))
                {
                    Log.Warn($"[AUTH] REFRESH RESPONSE but token is empty. elapsedMs={ElapsedMs(started)}");
                    return false;
                }

                TokenStore.Set(newToken!,
                               res?.ExpiresInSeconds,
                               res?.RefreshToken,
                               res?.AccessTokenExpiresAtUtc,
                               res?.RefreshTokenExpiresAtUtc);

                Log.Info($"[AUTH] REFRESH SUCCESS expiresIn={res?.ExpiresInSeconds}, elapsedMs={ElapsedMs(started)}");
                return true;
            }
            catch (ApiException apiEx)
            {
                Log.Error(apiEx,
                    $"[AUTH][ApiException][REFRESH] elapsedMs={ElapsedMs(started)}\n" +
                    BuildExceptionTree(apiEx));
                return false;
            }
            catch (HttpRequestException httpEx)
            {
                Log.Error(httpEx,
                    $"[AUTH][HttpRequestException][REFRESH] httpStatus={(int?)httpEx.StatusCode} ({httpEx.StatusCode}), elapsedMs={ElapsedMs(started)}\n" +
                    BuildExceptionTree(httpEx));
                return false;
            }
            catch (AuthenticationException tlsEx)
            {
                Log.Error(tlsEx,
                    $"[AUTH][TLS][REFRESH] Handshake/SSL hatası. elapsedMs={ElapsedMs(started)}\n" +
                    BuildExceptionTree(tlsEx));
                return false;
            }
            catch (TaskCanceledException tce)
            {
                Log.Error(tce,
                    $"[AUTH][Timeout][REFRESH] İstek zaman aşımı. elapsedMs={ElapsedMs(started)}\n" +
                    BuildExceptionTree(tce));
                return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    $"[AUTH][Unhandled][REFRESH] Beklenmeyen hata. elapsedMs={ElapsedMs(started)}\n" +
                    BuildExceptionTree(ex));
                return false;
            }
            finally
            {
                Log.Info($"[AUTH] REFRESH END   totalMs={ElapsedMs(started)}");
            }
        }
        private static string MaskId(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "(empty)";
            if (s.Length <= 2) return new string('*', s.Length);
            return new string('*', s.Length - 2) + s[^2..];
        }

        private static long ElapsedMs(DateTimeOffset start) =>
            (long)(DateTimeOffset.UtcNow - start).TotalMilliseconds;

        private static string BuildExceptionTree(Exception ex)
        {
            var sb = new StringBuilder();
            int i = 0;
            for (var cur = ex; cur != null; cur = cur.InnerException, i++)
            {
                sb.AppendLine($"-- Inner[{i}] {cur.GetType().FullName}");
                sb.AppendLine($"   Message: {cur.Message}");
                if (!string.IsNullOrWhiteSpace(cur.Source))
                    sb.AppendLine($"   Source : {cur.Source}");
                if (!string.IsNullOrWhiteSpace(cur.StackTrace))
                    sb.AppendLine("   Stack  : " + cur.StackTrace);
            }
            return sb.ToString();
        }
    }
}
