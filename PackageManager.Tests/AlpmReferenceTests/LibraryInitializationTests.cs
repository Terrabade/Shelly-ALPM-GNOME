using PackageManager.Alpm.Native;

namespace PackageManager.Tests.AlpmReferenceTests;

public class LibraryInitializationTests
{
    [Test]
    public void InitializeReturnsNonNullHandle()
    {
        var handle = AlpmReference.Initialize("/","/var/lib/pacman",out var error);
        Assert.That(handle, Is.Not.EqualTo(IntPtr.Zero));
        var result = AlpmReference.Release(handle);
        
    }
}