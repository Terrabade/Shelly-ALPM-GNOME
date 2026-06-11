using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PackageManager.Utilities.PkgBuild;

public enum ValidationSeverity
{
    Info,
    Warning,
    Critical
}

public class ValidationFinding
{

    public required string Tool { get; init; }

    public ValidationSeverity Severity { get; init; }

    public required string Hook { get; init; }


    public required string MatchedLine { get; init; }

    public required string Message { get; init; }
}

public class ValidationResult
{
    public bool HasFindings => Findings.Count > 0;

    public List<ValidationFinding> Findings { get; } = new();
}

/// <summary>
/// Inspects the resolved install scriptlets of a PKGBUILD for invocations of
/// package managers / network-fetching tools (npm and similar) that fetch and
/// execute arbitrary code at install time, outside of pacman's control.
/// </summary>
public class PostInstallValidator
{
    /// <summary>
    /// Tools considered risky when invoked inside an install scriptlet.
    /// Exposed as a settable property so the set can be extended or overridden.
    /// </summary>
    private IReadOnlyList<string> RiskyTools { get; init; } =
    [
        "npm", "npx", "yarn", "pnpm", "bun",
        "pip", "pip3", "pipx", "uv",
        "gem",
        "cargo install",
        "go install",
        "curl", "wget"
    ];

    public ValidationResult Validate(PkgbuildInfo info)
    {
        var result = new ValidationResult();
        ScanHook(info.PostInstall, "post_install", result);
        return result;
    }

    private void ScanHook(string? scriptlet, string hook, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(scriptlet))
            return;

        foreach (var rawLine in scriptlet.Split('\n'))
        {
            var line = StripShellComment(rawLine).Trim();
            if (line.Length == 0)
                continue;

            foreach (var tool in RiskyTools)
            {
                // Word-boundary match so "npm" matches `npm i` but not `npmrc`.
                if (Regex.IsMatch(line, $@"(^|[\s;&|`(]){Regex.Escape(tool)}(\s|$)"))
                {
                    result.Findings.Add(new ValidationFinding
                    {
                        Tool = tool,
                        Hook = hook,
                        Severity = ValidationSeverity.Warning,
                        MatchedLine = line,
                        Message = $"'{tool}' is invoked in {hook}() — "
                                  + "this fetches/executes external code outside pacman's control."
                    });
                }
            }
        }
    }

    /// <summary>
    /// Removes a trailing shell comment from a line, respecting single and
    /// double quotes so `#` inside a string is not treated as a comment.
    /// </summary>
    private static string StripShellComment(string line)
    {
        var inSingleQ = false;
        var inDoubleQ = false;
        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (c == '"' && !inSingleQ) inDoubleQ = !inDoubleQ;
            else if (c == '\'' && !inDoubleQ) inSingleQ = !inSingleQ;
            else if (c == '#' && !inSingleQ && !inDoubleQ)
                return line.Substring(0, i);
        }
        return line;
    }
}
