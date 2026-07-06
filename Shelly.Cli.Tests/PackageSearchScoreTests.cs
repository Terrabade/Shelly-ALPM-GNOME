using Shelly.Utilities;

namespace Shelly.Cli.Tests;

[TestFixture]
public class PackageSearchScoreTests
{
    // ---- Empty / whitespace search returns the full set (Score == 1) --------------

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Score_EmptySearch_ReturnsOne(string? search)
    {
        Assert.That(PackageSearch.Score("discord", "chat app", search), Is.EqualTo(1));
    }

    // ---- Tier values --------------------------------------------------------------

    [Test]
    public void Score_NameExact_Returns1000()
    {
        Assert.That(PackageSearch.Score("discord", "chat", "discord"), Is.EqualTo(1000));
    }

    [Test]
    public void Score_NamePrefix_Returns800()
    {
        Assert.That(PackageSearch.Score("discord-screenaudio", "chat", "discord"), Is.EqualTo(800));
    }

    [Test]
    public void Score_NameWholeWord_Returns600()
    {
        Assert.That(PackageSearch.Score("python-discord-utils", "chat", "discord"), Is.EqualTo(600));
    }

    [Test]
    public void Score_NameSubstring_Returns400()
    {
        Assert.That(PackageSearch.Score("legcord", "chat", "cord"), Is.EqualTo(400));
    }

    [Test]
    public void Score_DescriptionPrefix_Returns200()
    {
        Assert.That(PackageSearch.Score("vesktop", "Discord client", "discord"), Is.EqualTo(200));
    }

    [Test]
    public void Score_DescriptionWholeWord_Returns150()
    {
        Assert.That(PackageSearch.Score("vesktop", "A custom discord client", "discord"), Is.EqualTo(150));
    }

    [Test]
    public void Score_DescriptionSubstring_Returns100()
    {
        Assert.That(PackageSearch.Score("webcord", "A discordlike app", "discord"), Is.EqualTo(100));
    }

    [Test]
    public void Score_NoMatch_ReturnsZero()
    {
        Assert.That(PackageSearch.Score("firefox", "web browser", "discord"), Is.EqualTo(0));
    }

    // ---- Ordering: name tiers rank above description tiers ------------------------

    [Test]
    public void Score_NameMatchOutranksDescriptionMatch()
    {
        var nameHit = PackageSearch.Score("discord", "chat", "discord");
        var descHit = PackageSearch.Score("vesktop", "A discord client", "discord");
        Assert.That(nameHit, Is.GreaterThan(descHit));
    }

    // ---- Null description scored on name only (local packages) --------------------

    [Test]
    public void Score_NullDescription_ScoresOnNameOnly()
    {
        Assert.That(PackageSearch.Score("discord", null, "discord"), Is.EqualTo(1000));
        Assert.That(PackageSearch.Score("firefox", null, "discord"), Is.EqualTo(0));
    }

    // ---- Case-insensitivity -------------------------------------------------------

    [Test]
    public void Score_CaseInsensitive()
    {
        Assert.That(PackageSearch.Score("discord", "chat", "DISCORD"), Is.EqualTo(1000));
    }

    // ---- Matches mirrors Score > 0 ------------------------------------------------

    [Test]
    public void Matches_IsScoreGreaterThanZero()
    {
        Assert.That(PackageSearch.Matches("discord", "chat", "discord"), Is.True);
        Assert.That(PackageSearch.Matches("firefox", "web browser", "discord"), Is.False);
    }
}
