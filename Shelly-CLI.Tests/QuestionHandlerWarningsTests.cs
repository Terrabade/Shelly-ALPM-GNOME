using PackageManager.Aur;
using PackageManager.Utilities.PkgBuild;
using Shelly_CLI.Utility;

namespace Shelly_CLI.Tests;

[TestFixture]
public class QuestionHandlerWarningsTests
{
    private static PkgbuildDiffRequestEventArgs MakeArgs(params ValidationFinding[] findings) =>
        new()
        {
            PackageName = "some-package",
            OldPkgbuild = "old",
            NewPkgbuild = "new",
            Warnings = [.. findings],
        };

    private static ValidationFinding NpmFinding() => new()
    {
        Tool = "npm",
        Hook = "post_install",
        Severity = ValidationSeverity.Warning,
        MatchedLine = "npm install -g foo",
        Message = "'npm' is invoked in post_install().",
    };

    [Test]
    public void NoConfirm_WithWarnings_StillAutoApproves()
    {
        var args = MakeArgs(NpmFinding());

        QuestionHandler.HandlePkgbuildDiff(args, uiMode: false, noConfirm: true);

        // Minimal policy: warnings are surfaced/logged but noConfirm still proceeds.
        Assert.That(args.ProceedWithUpdate, Is.False);
    }

    [Test]
    public void NoConfirm_WithoutWarnings_AutoApproves()
    {
        var args = MakeArgs();

        QuestionHandler.HandlePkgbuildDiff(args, uiMode: false, noConfirm: true);

        Assert.That(args.ProceedWithUpdate, Is.True);
    }
}
