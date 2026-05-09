using System.Diagnostics;

namespace Shelly.Gtk.Services;

public sealed class FingerprintAuthDetector : IFingerprintAuthDetector
{
    private static readonly string[] PamFiles =
    [
        "/etc/pam.d/sudo",
        "/etc/pam.d/sudo-i",
        "/etc/pam.d/polkit-1",
    ];

    private readonly string[] _pamFiles;

    public FingerprintAuthDetector() : this(PamFiles)
    {
    }

    public FingerprintAuthDetector(string[] pamFiles)
    {
        _pamFiles = pamFiles;
    }

    public FingerprintDetectionResult Detect()
    {
        var hits = new List<string>();
        foreach (var path in _pamFiles)
        {
            if (!File.Exists(path)) continue;
            try
            {
                foreach (var raw in File.ReadAllLines(path))
                {
                    var line = raw.TrimStart();
                    if (line.StartsWith('#')) continue;
                    if (!line.Contains("pam_fprintd.so", StringComparison.Ordinal)) continue;
                    hits.Add(path);
                    break;
                }
            }
            catch(Exception e)
            {
                Console.WriteLine($"Failed to read pam file {path} {e}");
            }
        }

        var sudoHit = hits.Any(p => p.EndsWith("/sudo") || p.EndsWith("/sudo-i"));
        return new FingerprintDetectionResult(sudoHit, hits);
    }

    public bool FprintdServiceActive()
    {
        try
        {
            var psi = new ProcessStartInfo("systemctl")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add("is-active");
            psi.ArgumentList.Add("--quiet");
            psi.ArgumentList.Add("fprintd.service");
            using var p = Process.Start(psi);
            if (p == null) return false;
            p.WaitForExit(2000);
            return p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
