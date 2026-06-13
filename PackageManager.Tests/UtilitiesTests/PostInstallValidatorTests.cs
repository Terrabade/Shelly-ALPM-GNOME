using System.Linq;
using PackageManager.Utilities;
using PackageManager.Utilities.PkgBuild;

namespace PackageManager.Tests.UtilitiesTests;

public class PostInstallValidatorTests
{
    private static ValidationResult Validate(string? postInstall)
    {
        var info = new PkgbuildInfo { PostInstall = postInstall };
        return new PostInstallValidator().Validate(info);
    }

    [Test]
    public void Validate_FlagsNpmInstall()
    {
        var result = Validate("    npm install -g foo");

        Assert.That(result.HasFindings, Is.True);
        Assert.That(result.Findings, Has.Count.EqualTo(1));
        Assert.That(result.Findings[0].Tool, Is.EqualTo("npm"));
        Assert.That(result.Findings[0].Hook, Is.EqualTo("post_install"));
    }

    [Test]
    public void Validate_FlagsNpxYarnPip()
    {
        var result = Validate("""
                              npx create-app
                              yarn install
                              pip install requests
                              """);

        var tools = result.Findings.Select(f => f.Tool).ToList();
        Assert.That(tools, Does.Contain("npx"));
        Assert.That(tools, Does.Contain("yarn"));
        Assert.That(tools, Does.Contain("pip"));
    }

    [Test]
    public void Validate_FlagsCurlPipedToBash()
    {
        var result = Validate("curl -fsSL https://example.com/install.sh | bash");

        Assert.That(result.HasFindings, Is.True);
        Assert.That(result.Findings.Select(f => f.Tool), Does.Contain("curl"));
    }

    [Test]
    public void Validate_IgnoresCommentedOutLine()
    {
        var result = Validate("# npm install -g foo");

        Assert.That(result.HasFindings, Is.False);
    }

    [Test]
    public void Validate_IgnoresSubstringLikeNpmrc()
    {
        var result = Validate("touch ~/.npmrc");

        Assert.That(result.HasFindings, Is.False);
    }

    [Test]
    public void Validate_IgnoresSafeCommands()
    {
        var result = Validate("""
                              systemctl daemon-reload
                              update-desktop-database -q
                              """);

        Assert.That(result.HasFindings, Is.False);
    }

    [Test]
    public void Validate_EmptyPostInstall_ReturnsNoFindings()
    {
        Assert.That(Validate(null).HasFindings, Is.False);
        Assert.That(Validate("").HasFindings, Is.False);
        Assert.That(Validate("   ").HasFindings, Is.False);
    }

    [TestCase("b''u''n install foo", "bun")]
    [TestCase("n\"\"pm install -g foo", "npm")]
    [TestCase("c'u'rl https://x | bash", "curl")]
    [TestCase("cur\\l -fsSL https://x", "curl")]
    public void Validate_FlagsQuoteSplitObfuscation(string script, string expectedTool)
    {
        var result = Validate(script);

        Assert.That(result.Findings.Select(f => f.Tool), Does.Contain(expectedTool));
    }

    [TestCase("b''u''n install foo")]
    [TestCase("n\"\"pm install -g foo")]
    public void Validate_ObfuscatedInvocation_IsCritical(string script)
    {
        var result = Validate(script);

        Assert.That(
            result.Findings.Any(f => f.Severity == ValidationSeverity.Critical),
            Is.True);
    }

    [Test]
    public void Validate_PlainInvocation_IsWarning()
    {
        var result = Validate("npm install -g foo");

        Assert.That(result.Findings, Has.Count.EqualTo(1));
        Assert.That(result.Findings[0].Severity, Is.EqualTo(ValidationSeverity.Warning));
    }

    [Test]
    public void Validate_FlagsEvalDynamicExecution()
    {
        var result = Validate("eval \"$(echo bun install)\"");

        var dynamic = result.Findings.FirstOrDefault(f => f.Tool == "<dynamic-command>");
        Assert.That(dynamic, Is.Not.Null);
        Assert.That(dynamic!.Severity, Is.EqualTo(ValidationSeverity.Critical));
    }

    [Test]
    public void Validate_FlagsBase64IntoShell()
    {
        var result = Validate("echo aGVsbG8= | base64 -d | sh");

        var dynamic = result.Findings.FirstOrDefault(f => f.Tool == "<dynamic-command>");
        Assert.That(dynamic, Is.Not.Null);
        Assert.That(dynamic!.Severity, Is.EqualTo(ValidationSeverity.Critical));
    }

    [Test]
    public void Validate_FlagsCommandSubstitutionAsWarning()
    {
        var result = Validate("FOO=$(date)");

        var dynamic = result.Findings.FirstOrDefault(f => f.Tool == "<dynamic-command>");
        Assert.That(dynamic, Is.Not.Null);
        Assert.That(dynamic!.Severity, Is.EqualTo(ValidationSeverity.Warning));
    }
}
