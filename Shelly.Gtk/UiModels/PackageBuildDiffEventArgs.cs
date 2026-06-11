namespace Shelly.Gtk.UiModels;

public class PackageBuildDiffEventArgs(string packageName, IReadOnlyList<string> diffLines) : EventArgs{  
    
    private readonly TaskCompletionSource<bool> _tcs = new();    
    
    public Task<bool> ResponseTask => _tcs.Task;

    public IReadOnlyList<string>? DiffLines { get; } = diffLines;

    public string PackageName  { get; } = packageName;    
    
    public void SetResponse(bool response)
    {
        _tcs.TrySetResult(response);
    }
    
}  