namespace Shelly.Gtk.Helpers;

public static class ExtractAppIdHelper
{
    internal static string ExtractAppId(string raw)
    {
        if (raw.StartsWith("appstream://"))
            return raw["appstream://".Length..];

        if (raw.StartsWith("flatpak+https://"))
            return raw.Split('/')
                .Last()
                .Replace(".flatpakref", "");

        return raw;
    }
}