using System.Text.RegularExpressions;
using Shelly.Cli.Commands;
using Shelly.Cli.Outputs;

namespace Shelly.Cli.Tests;

[TestFixture]
public class ShellyFileLogTests
{
    // Mirror of the reader-side contract in
    // Shelly.Gtk/Services/OperationLogService.cs — these MUST keep matching.
    private static readonly Regex SessionStart =
        new(@"\[(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})\] SESSION START");
    private static readonly Regex Command = new(@"Command: (.+)$");
    private static readonly Regex User = new(@"User: (.+) \(sudo: (yes|no)\)");
    private static readonly Regex SessionEnd = new(@"SESSION END — exit code: (\d+)");

    [Test]
    public void WritesReaderCompatibleSession()
    {
        var dir = Path.Combine(Path.GetTempPath(), "shelly-log-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var logPath = Path.Combine(dir, "shelly.log");
        var rotatedPath = Path.Combine(dir, "shelly.log.1");

        try
        {
            using (var log = ShellyFileLog.TryOpen(logPath, rotatedPath))
            {
                Assert.That(log, Is.Not.Null);
                log!.WriteSessionHeader(["upgrade", "all"]);
                log.WriteLine("doing work");
                log.WriteSessionFooter(0);
            }

            var lines = File.ReadAllLines(logPath);

            Assert.Multiple(() =>
            {
                Assert.That(lines.Any(l => SessionStart.IsMatch(l)), Is.True, "SESSION START marker");
                Assert.That(lines.Any(l => Command.IsMatch(l)), Is.True, "Command marker");
                Assert.That(lines.Any(l => User.IsMatch(l)), Is.True, "User marker");

                var end = lines.Select(l => SessionEnd.Match(l)).FirstOrDefault(m => m.Success);
                Assert.That(end, Is.Not.Null);
                Assert.That(end!.Groups[1].Value, Is.EqualTo("0"));
            });
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public void LoggingShellyConsoleTeesOutputToLog()
    {
        var dir = Path.Combine(Path.GetTempPath(), "shelly-log-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var logPath = Path.Combine(dir, "shelly.log");
        var rotatedPath = Path.Combine(dir, "shelly.log.1");

        try
        {
            using (var log = ShellyFileLog.TryOpen(logPath, rotatedPath))
            {
                Assert.That(log, Is.Not.Null);
                IShellyConsole console = new LoggingShellyConsole(new SystemShellyConsole(), log!);
                console.WriteLine("hello-from-command");
            }

            var content = File.ReadAllText(logPath);
            Assert.That(content, Does.Contain("hello-from-command"));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Test]
    public void FactoryDoesNotRecurse_ReturnsPlainConsoleWhenNoLog()
    {
        ShellyFileLog.Current = null;

        var console = ShellyConsoleFactory.Create();

        Assert.That(console, Is.Not.Null);
        Assert.That(console, Is.InstanceOf<SystemShellyConsole>());
    }

    [Test]
    public void FactoryWrapsInLoggingConsoleWhenLogActive()
    {
        var dir = Path.Combine(Path.GetTempPath(), "shelly-log-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var logPath = Path.Combine(dir, "shelly.log");
        var rotatedPath = Path.Combine(dir, "shelly.log.1");

        try
        {
            using var log = ShellyFileLog.TryOpen(logPath, rotatedPath);
            Assert.That(log, Is.Not.Null);
            ShellyFileLog.Current = log;

            var console = ShellyConsoleFactory.Create();

            Assert.That(console, Is.InstanceOf<LoggingShellyConsole>());
        }
        finally
        {
            ShellyFileLog.Current = null;
            Directory.Delete(dir, recursive: true);
        }
    }
}
