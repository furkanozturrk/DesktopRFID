using DesktopRFID.Data.Interfaces;
using System.Collections.Concurrent;
using System.Text;

namespace DesktopRFID.Infrastructure.Logging
{
    public sealed class FileLogger : IFileLogger, IDisposable
    {
#if DEBUG
        private static readonly string LogsDir =
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "logs"));
#else
        private static readonly string LogsDir =
            Path.Combine(AppContext.BaseDirectory, "logs");
#endif
        public static IFileLogger Default { get; } = new FileLogger();
        private readonly BlockingCollection<string> _queue = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _writerTask;
        public FileLogger()
        {
            Directory.CreateDirectory(LogsDir);
            _writerTask = Task.Factory.StartNew(
                WriterLoop,
                CancellationToken.None,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
        }
        private static string CurrentLogPath =>
            Path.Combine(LogsDir, $"Uygulama_{DateTime.UtcNow:yyyy-MM-dd}.log");
        public void Info(string message) => Enqueue("INFO", message);
        public void Warn(string message) => Enqueue("WARN", message);
        public void Error(string message) => Enqueue("ERROR", message);
        public void Error(Exception ex, string? message = null)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(message))
                sb.AppendLine(message);
            sb.AppendLine(ex.ToString());
            Enqueue("ERROR", sb.ToString().TrimEnd());
        }
        public void FlushAndStop()
        {
            try
            {
                _queue.CompleteAdding();
            }
            catch { }

            try
            {
                _cts.Cancel();
            }
            catch { }

            try
            {
                _writerTask.Wait(2000);
            }
            catch { }
        }
        public void Dispose()
        {
            FlushAndStop();
            _queue.Dispose();
            _cts.Dispose();
        }
        private void Enqueue(string level, string message)
        {
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            try { _queue.Add(line); } catch { }
        }
        private void WriterLoop()
        {
            try
            {
                foreach (var line in _queue.GetConsumingEnumerable(_cts.Token))
                {
                    for (int i = 0; i < 3; i++)
                    {
                        try
                        {
                            File.AppendAllText(CurrentLogPath, line + Environment.NewLine, Encoding.UTF8);
                            break;
                        }
                        catch (IOException)
                        {
                            Thread.Sleep(50);
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
        }
    }
}