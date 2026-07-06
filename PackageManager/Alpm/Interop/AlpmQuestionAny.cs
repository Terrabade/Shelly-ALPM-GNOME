using System.Runtime.InteropServices;

namespace PackageManager.Alpm.Interop;

[StructLayout(LayoutKind.Sequential)]
public struct AlpmQuestionAny
{
    public int Type;
    public int Answer;
}