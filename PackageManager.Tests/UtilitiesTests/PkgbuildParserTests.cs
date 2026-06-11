using PackageManager.Utilities;
using PackageManager.Utilities.PkgBuild;

namespace PackageManager.Tests.UtilitiesTests;

public class PkgbuildParserTests
{
    [Test]
    public void ParseContent_ResolvesSimpleVariableSubstitution_InDepends()
    {
        var pkgbuild = """
                       pkgname=simple-web-server
                       pkgver=1.2.17
                       pkgrel=1
                       _electronversion=38
                       depends=("electron${_electronversion}")
                       makedepends=('curl' 'gendesk' 'git' 'npm' 'nvm')
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.Depends, Has.Count.EqualTo(1));
        Assert.That(result.Depends[0], Is.EqualTo("electron38"));
    }

    [Test]
    public void ParseContent_ResolvesVariableWithoutBraces_InDepends()
    {
        var pkgbuild = """
                       _electronversion=38
                       depends=("electron$_electronversion")
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.Depends, Has.Count.EqualTo(1));
        Assert.That(result.Depends[0], Is.EqualTo("electron38"));
    }

    [Test]
    public void ParseContent_ResolvesMultipleVariables_InSingleDep()
    {
        var pkgbuild = """
                       _pkgname=myapp
                       _ver=2
                       depends=("${_pkgname}-libs${_ver}")
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.Depends, Has.Count.EqualTo(1));
        Assert.That(result.Depends[0], Is.EqualTo("myapp-libs2"));
    }

    [Test]
    public void ParseContent_KeepsLiteralWhenVariableNotFound()
    {
        var pkgbuild = """
                       depends=("electron${_undefined}")
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.Depends, Has.Count.EqualTo(1));
        Assert.That(result.Depends[0], Is.EqualTo("electron${_undefined}"));
    }

    [Test]
    public void ParseContent_LeavesPlainDepsUnchanged()
    {
        var pkgbuild = """
                       depends=('pacman' 'gtk4' 'glib2')
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.Depends, Has.Count.EqualTo(3));
        Assert.That(result.Depends[0], Is.EqualTo("pacman"));
        Assert.That(result.Depends[1], Is.EqualTo("gtk4"));
        Assert.That(result.Depends[2], Is.EqualTo("glib2"));
    }

    [Test]
    public void ParseContent_ResolvesArrayExpansion()
    {
        var pkgbuild = """
                       _common_deps=('pacman' 'git')
                       depends=("${_common_deps[@]}" 'bash')
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.Depends, Has.Count.EqualTo(3));
        Assert.That(result.Depends, Does.Contain("pacman"));
        Assert.That(result.Depends, Does.Contain("git"));
        Assert.That(result.Depends, Does.Contain("bash"));
    }

    [Test]
    public void ParseContent_ResolvesChainedVariables()
    {
        var pkgbuild = """
                       _base=3
                       _ver=$_base
                       depends=("pkg>=$_ver")
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.Depends, Has.Count.EqualTo(1));
        Assert.That(result.Depends[0], Is.EqualTo("pkg>=3"));
    }

    [Test]
    public void ParseContent_EvaluatesArithmetic()
    {
        var pkgbuild = """
                       _major=3
                       _ver=$((1+2))
                       depends=("pkg>=$_ver")
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.Depends[0], Is.EqualTo("pkg>=3"));
    }

    [Test]
    public void ParseContent_SkipsConditionalDependsBlock()
    {
        var pkgbuild = """
                       depends=('base-pkg')
                       if [[ $SOME_VAR == 'ON' ]]; then
                         depends+=('conditional-pkg')
                       fi
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.Depends, Has.Count.EqualTo(1));
        Assert.That(result.Depends[0], Is.EqualTo("base-pkg"));
    }

    [Test]
    public void ParseContent_IncludesNonConditionalPlusEquals()
    {
        var pkgbuild = """
                       depends=('base-pkg')
                       depends+=('extra-pkg')
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.Depends, Has.Count.EqualTo(2));
        Assert.That(result.Depends, Does.Contain("base-pkg"));
        Assert.That(result.Depends, Does.Contain("extra-pkg"));
    }

    [Test]
    public void ParseContent_StripsUnresolvedVersionConstraint()
    {
        // Simulates command substitution that can't be resolved
        var pkgbuild = """
                       _ver=$(pkg-config --modversion foo)
                       depends=("pkg>=$_ver")
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        // Should strip the dangling >= and just return "pkg"
        Assert.That(result.Depends[0], Is.EqualTo("pkg"));
    }

    [Test]
    public void ParseContent_FfmpegObsStyle_ResolvesVersionedDeps()
    {
        var pkgbuild = """
                       _aomver=3
                       _srtver=1.5
                       _dav1dver=1.3.0
                       depends=(
                         "aom>=$_aomver"
                         "srt>=$_srtver"
                         "dav1d>=$_dav1dver"
                         alsa-lib
                       )
                       if [[ $FFMPEG_OBS_SVT == 'ON' ]]; then
                         depends+=("svt-av1>=4")
                       fi
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.Depends, Has.Count.EqualTo(4));
        Assert.That(result.Depends[0], Is.EqualTo("aom>=3"));
        Assert.That(result.Depends[1], Is.EqualTo("srt>=1.5"));
        Assert.That(result.Depends[2], Is.EqualTo("dav1d>=1.3.0"));
        Assert.That(result.Depends[3], Is.EqualTo("alsa-lib"));
    }

    [Test]
    public void ParseContent_OptDepends_PreservesLiteralParenInsideQuotes()
    {
        // Regression test for proton-ge-custom-bin: a quoted optdepends entry that
        // contains a literal ')' inside its description (e.g. a URL) must not
        // truncate the array. Previously the regex `[^)]*` stopped at the first
        // ')' character, causing description fragments to leak as bogus package
        // names ("needed", "by", "protonfixes", "(https://github.com/...", etc.).
        var pkgbuild = """
                       pkgname=proton-ge-custom-bin
                       optdepends=(
                         'zenity: needed by protonfixes for the gui'
                         'python-kivy: needed by protonfixes for the gui (https://github.com/kivy/kivy)'
                         'winetricks: variety of fixes for games'
                       )
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.OptDepends, Has.Count.EqualTo(3));
        Assert.That(result.OptDepends[0], Is.EqualTo("zenity: needed by protonfixes for the gui"));
        Assert.That(result.OptDepends[1], Is.EqualTo("python-kivy: needed by protonfixes for the gui (https://github.com/kivy/kivy)"));
        Assert.That(result.OptDepends[2], Is.EqualTo("winetricks: variety of fixes for games"));
    }

    [Test]
    public void ParseContent_OptDepends_HandlesMixedQuotesWithParen()
    {
        var pkgbuild = """
                       optdepends=(
                         "bar: paren ) inside double quotes"
                         'baz: paren ) inside single quotes'
                       )
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.OptDepends, Has.Count.EqualTo(2));
        Assert.That(result.OptDepends[0], Is.EqualTo("bar: paren ) inside double quotes"));
        Assert.That(result.OptDepends[1], Is.EqualTo("baz: paren ) inside single quotes"));
    }

    [Test]
    public void ParseContent_OptDepends_HandlesAppendForm()
    {
        var pkgbuild = """
                       optdepends=('a: first')
                       optdepends+=('b: second (with paren)')
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.OptDepends, Has.Count.EqualTo(2));
        Assert.That(result.OptDepends[0], Is.EqualTo("a: first"));
        Assert.That(result.OptDepends[1], Is.EqualTo("b: second (with paren)"));
    }

    [Test]
    public void ParseContent_Array_UnterminatedDoesNotThrow()
    {
        // Malformed PKGBUILD missing closing ')': must not throw, should capture
        // whatever it found.
        var pkgbuild = "optdepends=(\n  'a: ok'\n  'b: also ok'\n";

        Assert.DoesNotThrow(() => PkgbuildParser.ParseContent(pkgbuild));
    }

    // ---- install= / post_install handling ----

    [Test]
    public void ParseContent_NoInstallDirective_LeavesInstallFileAndPostInstallNull()
    {
        var pkgbuild = """
                       pkgname=myapp
                       pkgver=1.0
                       depends=('bash')
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.InstallFile, Is.Null);
        Assert.That(result.PostInstall, Is.Null);
    }

    [Test]
    public void ParseContent_ResolvesInstallFileWithVariableSubstitution()
    {
        var pkgbuild = """
                       pkgname=myapp
                       pkgver=1.0
                       install=${pkgname}.install
                       """;

        // No baseDir provided -> install file lookup is relative and won't exist,
        // so PostInstall is null but InstallFile must still be resolved.
        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.InstallFile, Is.EqualTo("myapp.install"));
        Assert.That(result.PostInstall, Is.Null);
    }

    [Test]
    public void ParseContent_ResolvesQuotedInstallFile()
    {
        var pkgbuild = """
                       pkgname=foo
                       install="foo.install"
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.InstallFile, Is.EqualTo("foo.install"));
    }

    [Test]
    public void ParseContent_ExtractsPostInstallBody_FromInstallFileInBaseDir()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "shelly_pkgbuild_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var installContent = """
                                 post_install() {
                                     echo "installing"
                                     update-desktop-database -q
                                 }

                                 post_upgrade() {
                                     echo "upgrading"
                                 }
                                 """;
            File.WriteAllText(Path.Combine(tempDir, "myapp.install"), installContent);

            var pkgbuild = """
                           pkgname=myapp
                           install=${pkgname}.install
                           """;

            var result = PkgbuildParser.ParseContent(pkgbuild, tempDir);

            Assert.That(result.InstallFile, Is.EqualTo("myapp.install"));
            Assert.That(result.PostInstall, Is.Not.Null);
            Assert.That(result.PostInstall, Does.Contain("echo \"installing\""));
            Assert.That(result.PostInstall, Does.Contain("update-desktop-database -q"));
            // Body of post_upgrade must not leak into post_install.
            Assert.That(result.PostInstall, Does.Not.Contain("upgrading"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void ParseContent_ExtractsInlinePostInstall_WhenNoInstallFile()
    {
        var pkgbuild = """
                       pkgname=myapp
                       pkgver=1.0

                       post_install() {
                           npm install -g foo
                       }
                       """;

        var result = PkgbuildParser.ParseContent(pkgbuild);

        Assert.That(result.PostInstall, Is.Not.Null);
        Assert.That(result.PostInstall, Does.Contain("npm install -g foo"));

        var findings = new PostInstallValidator().Validate(result).Findings;
        Assert.That(findings, Has.Some.Matches<ValidationFinding>(f => f.Tool == "npm"));
    }

    [Test]
    public void InlinePostInstall_RiskyTool_ProducesValidatorFinding()
    {
        // Fake PKGBUILD with NO install= file — only an inline post_install().
        var pkgbuild = """
                       pkgname=fakeapp
                       pkgver=1.0
                       pkgrel=1
                       arch=('any')

                       post_install() {
                           echo "setting up"
                           npm install -g fakeapp
                       }
                       """;

        // Parse (no baseDir, so the inline fallback is what populates PostInstall).
        var info = PkgbuildParser.ParseContent(pkgbuild);
        Assert.That(info.PostInstall, Is.Not.Null);
        Assert.That(info.PostInstall, Does.Contain("npm install -g fakeapp"));

        // The "effect": validator flags the npm invocation.
        var result = new PostInstallValidator().Validate(info);

        Assert.That(result.HasFindings, Is.True);
        var finding = result.Findings.Single(f => f.Tool == "npm");
        Assert.Multiple(() =>
        {
            Assert.That(finding.Hook, Is.EqualTo("post_install"));
            Assert.That(finding.Severity, Is.EqualTo(ValidationSeverity.Warning));
            Assert.That(finding.MatchedLine, Is.EqualTo("npm install -g fakeapp"));
            Assert.That(finding.Message, Does.Contain("npm"));
        });
    }

    [Test]
    public void InlinePostInstall_SafeCommands_ProduceNoFindings()
    {
        var pkgbuild = """
                       pkgname=fakeapp
                       pkgver=1.0

                       post_install() {
                           systemctl daemon-reload
                           update-desktop-database -q
                           # npm install -g should-be-ignored
                       }
                       """;

        var info = PkgbuildParser.ParseContent(pkgbuild);
        var result = new PostInstallValidator().Validate(info);

        Assert.That(result.HasFindings, Is.False);
    }

    [Test]
    public void ParseContent_PostInstallNull_WhenFunctionAbsentInInstallFile()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "shelly_pkgbuild_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            File.WriteAllText(
                Path.Combine(tempDir, "bar.install"),
                "post_upgrade() {\n  echo hi\n}\n");

            var pkgbuild = """
                           pkgname=bar
                           install=bar.install
                           """;

            var result = PkgbuildParser.ParseContent(pkgbuild, tempDir);

            Assert.That(result.InstallFile, Is.EqualTo("bar.install"));
            Assert.That(result.PostInstall, Is.Null);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void ParseContent_HandlesNestedBracesInPostInstallBody()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "shelly_pkgbuild_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var installContent = """
                                 post_install() {
                                     if [ -d /tmp ]; then
                                         echo "${HOME}"
                                     fi
                                 }
                                 """;
            File.WriteAllText(Path.Combine(tempDir, "nested.install"), installContent);

            var pkgbuild = """
                           pkgname=nested
                           install=nested.install
                           """;

            var result = PkgbuildParser.ParseContent(pkgbuild, tempDir);

            Assert.That(result.PostInstall, Is.Not.Null);
            Assert.That(result.PostInstall, Does.Contain("if [ -d /tmp ]; then"));
            Assert.That(result.PostInstall, Does.Contain("echo \"${HOME}\""));
            Assert.That(result.PostInstall, Does.Contain("fi"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Test]
    public void ParseContent_MissingInstallFile_DoesNotThrowAndPostInstallNull()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "shelly_pkgbuild_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var pkgbuild = """
                           pkgname=ghost
                           install=ghost.install
                           """;

            PkgbuildInfo? result = null;
            Assert.DoesNotThrow(() => result = PkgbuildParser.ParseContent(pkgbuild, tempDir));
            Assert.That(result!.InstallFile, Is.EqualTo("ghost.install"));
            Assert.That(result.PostInstall, Is.Null);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
