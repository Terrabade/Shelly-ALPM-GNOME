namespace PackageManager.Alpm.Events.EventArgs;

public class AlpmHookEventArgs(string description, ulong position, ulong total) : System.EventArgs
{
    public string Description { get; } = description;
    public ulong Position { get; } = position;
    public ulong Total { get; } = total;
}
