using System;

namespace PackageManager.Alpm.Enums;

[Flags]
public enum AlpmTransFlag : int
{
    None = 0,

    /// <summary>Ignore dependency checks.</summary>
    NoDeps = 1,

    /// <summary>Delete files even if they are tagged as backup.</summary>
    NoSave = (1 << 2),

    /// <summary>Ignore version numbers when checking dependencies.</summary>
    NoDepVersion = (1 << 3),

    /// <summary>Remove also any packages depending on a package being removed.</summary>
    Cascade = (1 << 4),

    /// <summary>Remove packages and their unneeded deps (not explicitly installed).</summary>
    Recurse = (1 << 5),

    /// <summary>Modify database but do not commit changes to the filesystem.</summary>
    DbOnly = (1 << 6),

    /// <summary>Do not run hooks during a transaction.</summary>
    NoHooks = (1 << 7),

    /// <summary>Use ALPM_PKG_REASON_DEPEND when installing packages.</summary>
    AllDeps = (1 << 8),

    /// <summary>Only download packages and do not actually install.</summary>
    DownloadOnly = (1 << 9),

    /// <summary>Do not execute install scriptlets after installing.</summary>
    NoScriptlet = (1 << 10),

    /// <summary>Ignore dependency conflicts.</summary>
    NoUpgrade = (1 << 11),

    /// <summary>Do not extract files from the package.</summary>
    NoExtract = (1 << 12),

    /// <summary>Do not check package signatures.</summary>
    NoPkgSig = (1 << 13),
    NoCheckSpace = (1 << 14)
}