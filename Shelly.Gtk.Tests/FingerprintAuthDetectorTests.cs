using Shelly.Gtk.Services;

namespace Shelly.Gtk.Tests;

[TestFixture]
public class FingerprintAuthDetectorTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "shelly-fpd-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private string WriteFixture(string name, string contents)
    {
        var path = Path.Combine(_tempDir, name);
        File.WriteAllText(path, contents);
        return path;
    }

    [Test]
    public void Detect_ReportsSudoFingerprint_WhenPamFprintdIsActive()
    {
        var sudo = WriteFixture("sudo",
            "#%PAM-1.0\nauth       sufficient   pam_fprintd.so\nauth       include      system-auth\n");

        var detector = new FingerprintAuthDetector([sudo]);
        var result = detector.Detect();

        Assert.That(result.SudoUsesFingerprint, Is.True);
        Assert.That(result.MatchingFiles, Does.Contain(sudo));
    }

    [Test]
    public void Detect_IgnoresCommentedLines()
    {
        var sudo = WriteFixture("sudo",
            "#%PAM-1.0\n#auth       sufficient   pam_fprintd.so\nauth       include      system-auth\n");

        var detector = new FingerprintAuthDetector([sudo]);
        var result = detector.Detect();

        Assert.That(result.SudoUsesFingerprint, Is.False);
        Assert.That(result.MatchingFiles, Is.Empty);
    }

    [Test]
    public void Detect_ReturnsFalse_WhenNoFprintdLine()
    {
        var sudo = WriteFixture("sudo",
            "#%PAM-1.0\nauth       include      system-auth\naccount    include      system-auth\n");

        var detector = new FingerprintAuthDetector([sudo]);
        var result = detector.Detect();

        Assert.That(result.SudoUsesFingerprint, Is.False);
        Assert.That(result.MatchingFiles, Is.Empty);
    }

    [Test]
    public void Detect_ReturnsFalse_WhenFileMissing()
    {
        var detector = new FingerprintAuthDetector([Path.Combine(_tempDir, "does-not-exist")]);
        var result = detector.Detect();

        Assert.That(result.SudoUsesFingerprint, Is.False);
        Assert.That(result.MatchingFiles, Is.Empty);
    }

    [Test]
    public void Detect_PolkitOnlyHit_DoesNotFlagSudo()
    {
        var polkit = WriteFixture("polkit-1",
            "#%PAM-1.0\nauth       sufficient   pam_fprintd.so\nauth       include      system-auth\n");

        var detector = new FingerprintAuthDetector([polkit]);
        var result = detector.Detect();

        Assert.That(result.SudoUsesFingerprint, Is.False);
        Assert.That(result.MatchingFiles, Does.Contain(polkit));
    }
}
