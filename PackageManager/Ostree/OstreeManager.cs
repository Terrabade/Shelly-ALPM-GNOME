using System.Collections.Generic;

namespace PackageManager.Ostree;

public class OstreeManager
{
    public List<OstreeRef> ListRefs(string repoPath)
    {
        return [];
    }

    public bool DeleteRef(string repoPath, string remote, string reference)
    {
        return false;
    }

    public bool Prune(string repoPath)
    {
        return true;
    }

    public FsckResult Result(string repoPath)
    {
        return new FsckResult();
    }
}