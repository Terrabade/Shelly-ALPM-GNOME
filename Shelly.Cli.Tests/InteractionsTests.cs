using System;
using System.IO;
using NUnit.Framework;
using Shelly.Cli.Interactions;

namespace Shelly.Cli.Tests;

[TestFixture]
public class InteractionTests
{
    [Test]
    public void Execute_ListsOptionsStartingAtOne()
    {
        var input = new StringReader("2" + Environment.NewLine);
        var output = new StringWriter();
        Console.SetIn(input);
        Console.SetOut(output);

        var options = new[] { "alpha", "beta", "gamma" };

        var result = BasicSelection.Execute("Pick one", options);

        var written = output.ToString();
        Assert.That(written, Does.Contain("1. alpha"));
        Assert.That(written, Does.Contain("2. beta"));
        Assert.That(written, Does.Contain("3. gamma"));
        Assert.That(result, Is.EqualTo(1)); // user entered 2, so 0-based index 1
    }

    [Test]
    public void Execute_ReturnsDefaultIndexOnEmptyInput()
    {
        var input = new StringReader(Environment.NewLine);
        Console.SetOut(new StringWriter());
        Console.SetIn(input);

        var options = new[] { "alpha", "beta", "gamma" };

        var result = BasicSelection.Execute("Pick one", options, defaultIndex: 2);

        Assert.That(result, Is.EqualTo(2));
    }
}
