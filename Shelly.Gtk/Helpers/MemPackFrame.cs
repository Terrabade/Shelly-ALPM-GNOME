using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using MemoryPack;

namespace Shelly.Gtk.Helpers;

[SuppressMessage("Trimming",
    "IL2091:Target generic argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in target method or type. The generic parameter of the source method or type does not have matching annotations.")]
public static class MemPackFrame
{
    public const string Prefix = "[MEMPACK]";
    public const string Suffix = "[/MEMPACK]";

    public static bool TryDecode<T>(string output, out T? value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(output)) return false;
        var pref = output.IndexOf(Prefix, StringComparison.Ordinal);
        if(pref < 0) return false;
        var suff = output.IndexOf(Suffix,pref+Prefix.Length, StringComparison.Ordinal);
        if(suff < 0) return false;
        var payload = output.AsSpan(pref+Prefix.Length, suff-(pref+Prefix.Length));
        try
        {
            
            var bytes = Convert.FromBase64String(payload.ToString());
            // Commented out because it's too verbose' 
            //Console.Error.WriteLine($"[MemPackFrame] first16={Convert.ToHexString(bytes.AsSpan(0, Math.Min(16, bytes.Length)))}");
            value = MemoryPackSerializer.Deserialize<T>(bytes);
            return value is not null;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[MemPackFrame] decode failed: {ex.Message} (len={payload.Length}, mod4={payload.Length % 4})");
            return false;
        }
    }

    private static string StripBom(string s) =>
        s.Length > 0 && s[0] == '\uFEFF' ? s.Substring(1) : s;
}