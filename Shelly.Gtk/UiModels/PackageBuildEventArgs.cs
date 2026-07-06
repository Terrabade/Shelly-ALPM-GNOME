namespace Shelly.Gtk.UiModels;

public class PackageBuildEventArgs(string title, string pkgBuild,
    IReadOnlyDictionary<string, string>? sourceFiles = null) : EventArgs
{
    private readonly TaskCompletionSource<bool> _tcs = new();
    public Task<bool> ResponseTask => _tcs.Task;

    public string Title { get; } = title;
    public string PkgBuild { get; } = pkgBuild;
    public IReadOnlyDictionary<string, string> SourceFiles { get; } = sourceFiles ?? new Dictionary<string, string>();

    public void SetResponse(bool response)
    {
        _tcs.TrySetResult(response);
    }
}