using System.Collections.Generic;
using PackageManager.Ostree.Enums;

namespace PackageManager.Ostree;

public class FsckResult
{
    public FsckStatus Status {get;set;}
    
    public string Ref { get; set; } = "";
    
    public string Commit { get; set; } = "";
    
    public List<string> MissingObjects { get; set; }
    
    public List<string> InvalidObjects { get; set; }
    
    public string? errorMessage { get; set; }
    
    public bool isValid => Status == FsckStatus.Ok;
}