namespace DesktopRFID.Data.Dto;

public class AuthResult
{
    public bool Succeeded { get; init; }
    public string? Message { get; init; }
    public static AuthResult Ok(string? msg = null) => new() { Succeeded = true, Message = msg };
    public static AuthResult Fail(string msg) => new() { Succeeded = false, Message = msg };
}