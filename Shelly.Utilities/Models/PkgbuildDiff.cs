using System.Collections.Generic;

namespace Shelly.Utilities.Models;

/// <summary>
/// Classification of a single line in a unified PKGBUILD diff.
/// </summary>
public enum PkgbuildDiffKind
{
    Context,
    Added,
    Removed
}


public readonly record struct PkgbuildDiffLine(PkgbuildDiffKind Kind, string Text);


public static class PkgbuildDiff
{
    public static List<PkgbuildDiffLine> BuildLines(string oldText, string newText)
    {
        var oldLines = (oldText ?? string.Empty).Split('\n');
        var newLines = (newText ?? string.Empty).Split('\n');

        // Build LCS table.
        var lcs = new int[oldLines.Length + 1, newLines.Length + 1];
        for (int i = oldLines.Length - 1; i >= 0; i--)
        for (int j = newLines.Length - 1; j >= 0; j--)
            lcs[i, j] = oldLines[i].TrimEnd('\r') == newLines[j].TrimEnd('\r')
                ? lcs[i + 1, j + 1] + 1
                : System.Math.Max(lcs[i + 1, j], lcs[i, j + 1]);

        var result = new List<PkgbuildDiffLine>();
        int oi = 0, ni = 0;
        while (oi < oldLines.Length || ni < newLines.Length)
        {
            if (oi < oldLines.Length && ni < newLines.Length &&
                oldLines[oi].TrimEnd('\r') == newLines[ni].TrimEnd('\r'))
            {
                result.Add(new PkgbuildDiffLine(PkgbuildDiffKind.Context, oldLines[oi].TrimEnd('\r')));
                oi++;
                ni++;
            }
            else if (ni < newLines.Length &&
                     (oi >= oldLines.Length || lcs[oi, ni + 1] >= lcs[oi + 1, ni]))
            {
                result.Add(new PkgbuildDiffLine(PkgbuildDiffKind.Added, newLines[ni].TrimEnd('\r')));
                ni++;
            }
            else
            {
                result.Add(new PkgbuildDiffLine(PkgbuildDiffKind.Removed, oldLines[oi].TrimEnd('\r')));
                oi++;
            }
        }

        return result;
    }
}
