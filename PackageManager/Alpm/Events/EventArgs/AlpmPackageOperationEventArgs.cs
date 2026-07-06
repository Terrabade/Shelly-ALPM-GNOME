using PackageManager.Alpm.Enums;

namespace PackageManager.Alpm.Events.EventArgs;

public class AlpmPackageOperationEventArgs(AlpmEventType eventType, string? packageName) : System.EventArgs
{
    public AlpmEventType EventType { get; } = eventType;
    public string? PackageName { get; } = packageName;
}
