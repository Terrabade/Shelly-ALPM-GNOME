namespace Shelly.Gtk.UiModels;

public class PackageBuildDiffEventArgs(string packageName, string oldPkgbuild, string newPkgbuild, IReadOnlyList<string> diffLines) : EventArgs{  
    
    private readonly TaskCompletionSource<bool> _tcs = new();    
    
    public Task<bool> ResponseTask => _tcs.Task;

    public IReadOnlyList<string>? DiffLines { get; } = diffLines;

    public string PackageName  { get; } = packageName;    
    
    public string OldPkgbuild  { get; } = oldPkgbuild;    
    
    public string NewPkgbuild  { get; } = newPkgbuild;  
    
    public void SetResponse(bool response)
    {
        _tcs.TrySetResult(response);
    }
    
}  