using Shelly.Cli.Outputs;

namespace Shelly.Cli.Commands;

public sealed class LoggingShellyConsole(IShellyConsole inner, ShellyFileLog log) : IShellyConsole
{
    public TextWriter Output => inner.Output;
    public bool IsOutputRedirected => inner.IsOutputRedirected;

    public void Write(string text)
    {
        inner.Write(text);
        log.WriteLine(text);
    }

    public void WriteLine(string text)
    {
        inner.WriteLine(text);
        log.WriteLine(text);
    }

    public void WriteLine() => inner.WriteLine();
}

public static class ShellyConsoleFactory
{
    public static IShellyConsole Create()
    {
        IShellyConsole console = new SystemShellyConsole();
        var log = ShellyFileLog.Current;
        return log != null ? new LoggingShellyConsole(console, log) : console;
    }
}
