using System.Reflection;
using PackageManager.Aur;

namespace PackageManager.Tests.Aur;

public class AurPackageManagerTests
{
    [Test]
    public void SelectBuiltPackageFiles_ReturnsAllSplitPackageArtifacts_WhenRequestedPackageWasBuilt()
    {
        var tempPath = CreateTempDirectory();
        try
        {
            var requestedPackage = Path.Combine(tempPath, "starfish-1.0.0-1-x86_64.pkg.tar.zst");
            var dependentPackage = Path.Combine(tempPath, "starfish-data-1.0.0-1-any.pkg.tar.zst");
            var signature = Path.Combine(tempPath, "starfish-1.0.0-1-x86_64.pkg.tar.zst.sig");
            File.WriteAllText(requestedPackage, string.Empty);
            File.WriteAllText(dependentPackage, string.Empty);
            File.WriteAllText(signature, string.Empty);

            var result = SelectBuiltPackageFiles(tempPath, "starfish");

            Assert.That(result, Is.EquivalentTo(new[] { requestedPackage, dependentPackage }));
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Test]
    public void SelectBuiltPackageFiles_ReturnsNoArtifacts_WhenMultiplePackagesDoNotIncludeRequestedPackage()
    {
        var tempPath = CreateTempDirectory();
        try
        {
            File.WriteAllText(Path.Combine(tempPath, "other-1.0.0-1-x86_64.pkg.tar.zst"), string.Empty);
            File.WriteAllText(Path.Combine(tempPath, "other-data-1.0.0-1-any.pkg.tar.zst"), string.Empty);

            var result = SelectBuiltPackageFiles(tempPath, "starfish");

            Assert.That(result, Is.Empty);
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    [Test]
    public void SelectBuiltPackageFiles_ReturnsSingleArtifact_WhenOnlyOnePackageWasBuilt()
    {
        var tempPath = CreateTempDirectory();
        try
        {
            var onlyPackage = Path.Combine(tempPath, "custom-name-1.0.0-1-x86_64.pkg.tar.zst");
            File.WriteAllText(onlyPackage, string.Empty);

            var result = SelectBuiltPackageFiles(tempPath, "starfish");

            Assert.That(result, Is.EqualTo(new[] { onlyPackage }));
        }
        finally
        {
            Directory.Delete(tempPath, true);
        }
    }

    private static List<string> SelectBuiltPackageFiles(string tempPath, string packageName)
    {
        var method = typeof(AurPackageManager).GetMethod("SelectBuiltPackageFiles",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.That(method, Is.Not.Null);
        return (List<string>)method!.Invoke(null, [tempPath, packageName])!;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"shelly-aur-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }
}