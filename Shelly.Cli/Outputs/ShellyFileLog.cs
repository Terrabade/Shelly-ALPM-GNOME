namespace Shelly.Cli.Outputs;


public sealed class ShellyFileLog : IDisposable, IAsyncDisposable
{
    private const string LogPath = "/var/log/shelly.log";
    private const string RotatedLogPath = "/var/log/shelly.log.1";
    private const long MaxLogSizeBytes = 5 * 1024 * 1024; // 5MB

    private readonly StreamWriter _writer;
    private readonly Lock _gate = new();
    private bool _disposed;


    public static ShellyFileLog? Current { get; set; }

    private ShellyFileLog(StreamWriter writer) => _writer = writer;


    public static ShellyFileLog? TryOpen() => TryOpen(LogPath, RotatedLogPath);


    public static ShellyFileLog? TryOpen(string logPath, string rotatedLogPath)
    {
        try
        {
            RotateIfNeeded(logPath, rotatedLogPath);
            var writer = new StreamWriter(logPath, append: true) { AutoFlush = false };
            return new ShellyFileLog(writer);
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    public void WriteSessionHeader(string[] args)
    {
        var user = ResolveUser(out var isSudo);
        var now = DateTime.Now;

        lock (_gate)
        {
            if (_disposed) return;
            _writer.WriteLine("=====================================");
            _writer.WriteLine($"[{now:yyyy-MM-dd HH:mm:ss}] SESSION START");
            _writer.WriteLine($"[{now:yyyy-MM-dd HH:mm:ss}] Command: shelly {string.Join(' ', args)}");
            _writer.WriteLine($"[{now:yyyy-MM-dd HH:mm:ss}] User: {user} (sudo: {(isSudo ? "yes" : "no")})");
            _writer.WriteLine("=====================================");
            _writer.Flush();
        }
    }

    public void WriteSessionFooter(int exitCode)
    {
        lock (_gate)
        {
            if (_disposed) return;
            _writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SESSION END — exit code: {exitCode}");
            _writer.Flush();
        }
    }

    public void WriteLine(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        lock (_gate)
        {
            if (_disposed) return;
            _writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [OUT] {text}");
            _writer.Flush();
        }
    }

    private static string ResolveUser(out bool isSudo)
    {
        if (RootElevator.TryGetCallingUser(out var caller, out _))
        {
            isSudo = true;
            return caller;
        }

        isSudo = false;
        return Environment.UserName;
    }

    private static void RotateIfNeeded(string logPath, string rotatedLogPath)
    {
        try
        {
            if (!File.Exists(logPath)) return;

            var info = new FileInfo(logPath);
            if (info.Length < MaxLogSizeBytes) return;

            if (File.Exists(rotatedLogPath))
                File.Delete(rotatedLogPath);

            File.Move(logPath, rotatedLogPath);
        }
        catch
        {
            // Best effort — if rotation fails, continue logging to the existing file.
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _writer.Flush();
            _writer.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        StreamWriter? writer = null;
        lock (_gate)
        {
            if (!_disposed)
            {
                _disposed = true;
                writer = _writer;
            }
        }

        if (writer != null)
        {
            await writer.FlushAsync();
            await writer.DisposeAsync();
        }
    }
}
