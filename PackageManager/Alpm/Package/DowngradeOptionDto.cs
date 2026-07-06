namespace PackageManager.Alpm.Package;

public record struct DowngradeOptionDto(
    string Name,
    string Filename,
    string Location,
    bool IsInstalled
);