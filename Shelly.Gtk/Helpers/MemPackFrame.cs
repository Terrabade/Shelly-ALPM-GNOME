using System;
using System.Diagnostics.CodeAnalysis;
using MemoryPack;

namespace Shelly.Gtk.Helpers;

[SuppressMessage("Trimming",
    "IL2091:Target generic argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in target method or type. The generic parameter of the source method or type does not have matching annotations.")]
public static class MemPackFrame
{
    public const string Prefix = "[MEMPACK]";

    public static bool TryDecode<T>(string output, out T? value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(output)) return false;

        foreach (var raw in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var line = StripBom(raw.Trim());
            if (!line.StartsWith(Prefix, StringComparison.Ordinal)) continue;

            var payload = line.Substring(Prefix.Length);
            try
            {
                var bytes = Convert.FromBase64String(payload);
                value = MemoryPackSerializer.Deserialize<T>(bytes);
                return value is not null;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private static string StripBom(string s) =>
        s.Length > 0 && s[0] == '\uFEFF' ? s.Substring(1) : s;
}