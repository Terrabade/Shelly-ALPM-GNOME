namespace PackageManager.Alpm.Events.EventArgs;

public class AlpmScriptletEventArgs(string line) : System.EventArgs
{
    public string Line { get; } = line;
}
