
namespace DesktopRFID.Data.Interfaces
{
    public interface IFileLogger
    {
        void Info(string message);
        void Warn(string message);
        void Error(string message);
        void Error(Exception ex, string? message = null);
        void FlushAndStop();
    }
}