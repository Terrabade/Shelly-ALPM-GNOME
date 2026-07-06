using System.Runtime.InteropServices;
using PackageManager.Alpm;
using PackageManager.Alpm.Interop;
using PackageManager.Alpm.Package;

namespace PackageManager.Tests.AlpmTests;

[TestFixture]
public class AlpmPackageTests
{
    [Test]
    public void FromList_EmptyList_ReturnsEmptyList()
    {
        var result = AlpmPackage.FromList(IntPtr.Zero);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FromList_SingleElement_ReturnsOnePackage()
    {
        var pkgData = new IntPtr(0x1234);
        var listPtr = Marshal.AllocHGlobal(Marshal.SizeOf<AlpmList>());
        try
        {
            var listNode = new AlpmList
            {
                Data = pkgData,
                Next = IntPtr.Zero,
                Prev = IntPtr.Zero
            };
            Marshal.StructureToPtr(listNode, listPtr, false);

            var result = AlpmPackage.FromList(listPtr);

            Assert.That(result, Has.Count.EqualTo(1));
            // We can't easily verify the internal _pkgPtr without reflection or making it internal
            // but we can check if it returns a list with one item.
        }
        finally
        {
            Marshal.FreeHGlobal(listPtr);
        }
    }

    [Test]
    public void FromList_MultipleElements_ReturnsAllPackages()
    {
        var pkgData1 = new IntPtr(0x1111);
        var pkgData2 = new IntPtr(0x2222);
        var pkgData3 = new IntPtr(0x3333);

        int size = Marshal.SizeOf<AlpmList>();
        var listPtr1 = Marshal.AllocHGlobal(size);
        var listPtr2 = Marshal.AllocHGlobal(size);
        var listPtr3 = Marshal.AllocHGlobal(size);

        try
        {
            var node1 = new AlpmList { Data = pkgData1, Next = listPtr2, Prev = IntPtr.Zero };
            var node2 = new AlpmList { Data = pkgData2, Next = listPtr3, Prev = listPtr1 };
            var node3 = new AlpmList { Data = pkgData3, Next = IntPtr.Zero, Prev = listPtr2 };

            Marshal.StructureToPtr(node1, listPtr1, false);
            Marshal.StructureToPtr(node2, listPtr2, false);
            Marshal.StructureToPtr(node3, listPtr3, false);

            var result = AlpmPackage.FromList(listPtr1);

            Assert.That(result, Has.Count.EqualTo(3));
        }
        finally
        {
            Marshal.FreeHGlobal(listPtr1);
            Marshal.FreeHGlobal(listPtr2);
            Marshal.FreeHGlobal(listPtr3);
        }
    }

    [Test]
    public void FromList_NodeWithNullData_SkipsNode()
    {
        var pkgData1 = new IntPtr(0x1111);
        var pkgData2 = IntPtr.Zero;
        var pkgData3 = new IntPtr(0x3333);

        int size = Marshal.SizeOf<AlpmList>();
        var listPtr1 = Marshal.AllocHGlobal(size);
        var listPtr2 = Marshal.AllocHGlobal(size);
        var listPtr3 = Marshal.AllocHGlobal(size);

        try
        {
            var node1 = new AlpmList { Data = pkgData1, Next = listPtr2, Prev = IntPtr.Zero };
            var node2 = new AlpmList { Data = pkgData2, Next = listPtr3, Prev = listPtr1 };
            var node3 = new AlpmList { Data = pkgData3, Next = IntPtr.Zero, Prev = listPtr2 };

            Marshal.StructureToPtr(node1, listPtr1, false);
            Marshal.StructureToPtr(node2, listPtr2, false);
            Marshal.StructureToPtr(node3, listPtr3, false);

            var result = AlpmPackage.FromList(listPtr1);

            Assert.That(result, Has.Count.EqualTo(2));
        }
        finally
        {
            Marshal.FreeHGlobal(listPtr1);
            Marshal.FreeHGlobal(listPtr2);
            Marshal.FreeHGlobal(listPtr3);
        }
    }
}
