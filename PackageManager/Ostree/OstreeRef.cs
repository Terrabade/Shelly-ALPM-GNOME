namespace PackageManager.Ostree;

public class OstreeRef
{
    public string Remote { get; set; } = "";

    public string Ref { get; set; } = "";

    public string FullRef =>
        $"{Remote}:{Ref}";
}
