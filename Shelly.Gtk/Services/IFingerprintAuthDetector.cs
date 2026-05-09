namespace Shelly.Gtk.Services;

public interface IFingerprintAuthDetector
{
    FingerprintDetectionResult Detect();
    bool FprintdServiceActive();
}

public sealed record FingerprintDetectionResult(bool SudoUsesFingerprint, IReadOnlyList<string> MatchingFiles);
