using System.Linq;
using NUnit.Framework;
using Shelly.Utilities.Models;

namespace PackageManager.Tests.UtilitiesTests;

[TestFixture]
internal class PkgbuildDiffTests
{
    [Test]
    public void BuildLines_IdenticalText_AllContext()
    {
        const string text = "pkgname=foo\npkgver=1.0\n";

        var diff = PkgbuildDiff.BuildLines(text, text);

        Assert.That(diff.All(l => l.Kind == PkgbuildDiffKind.Context), Is.True);
    }

    [Test]
    public void BuildLines_ChangedLine_ProducesRemovedAndAdded()
    {
        const string oldText = "pkgname=foo\npkgver=1.0";
        const string newText = "pkgname=foo\npkgver=2.0";

        var diff = PkgbuildDiff.BuildLines(oldText, newText);

        Assert.Multiple(() =>
        {
            Assert.That(diff.Any(l => l.Kind == PkgbuildDiffKind.Removed && l.Text == "pkgver=1.0"), Is.True);
            Assert.That(diff.Any(l => l.Kind == PkgbuildDiffKind.Added && l.Text == "pkgver=2.0"), Is.True);
            Assert.That(diff.Any(l => l.Kind == PkgbuildDiffKind.Context && l.Text == "pkgname=foo"), Is.True);
        });
    }

    [Test]
    public void BuildLines_EmptyOld_FreshInstall_AllAdded()
    {
        const string newText = "pkgname=foo\npkgver=1.0\npkgrel=1";

        var diff = PkgbuildDiff.BuildLines(string.Empty, newText);

        // A fresh install has no cached PKGBUILD, so every meaningful line is an addition.
        Assert.That(diff.Where(l => l.Text.Length > 0).All(l => l.Kind == PkgbuildDiffKind.Added), Is.True);
        Assert.That(diff.Any(l => l.Kind == PkgbuildDiffKind.Added && l.Text == "pkgname=foo"), Is.True);
    }

    [Test]
    public void BuildLines_IgnoresTrailingCarriageReturn()
    {
        var diff = PkgbuildDiff.BuildLines("pkgver=1.0\r\n", "pkgver=1.0\n");

        Assert.That(diff.All(l => l.Kind == PkgbuildDiffKind.Context), Is.True);
    }
}
