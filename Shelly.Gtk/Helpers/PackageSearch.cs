using GObject;
using Gtk;
using static Shelly.GTK.Resources.Translations;

namespace Shelly.Gtk.Helpers;


internal static class PackageSearch
{

    public static int Score(string? name, string? description, string? search)
        => Shelly.Utilities.PackageSearch.Score(name, description, search);

    public static bool Matches(string? name, string? description, string? search)
        => Shelly.Utilities.PackageSearch.Matches(name, description, search);

    public static bool MatchesNameOrDescription(string? name, string? description, string? search)
        => Shelly.Utilities.PackageSearch.MatchesNameOrDescription(name, description, search);

    public static bool MatchesName(string? name, string? search)
        => Shelly.Utilities.PackageSearch.MatchesName(name, search);


    public static bool MatchesGroup(IEnumerable<string>? groups, string? selectedGroup)
    {
        if (string.IsNullOrEmpty(selectedGroup) || selectedGroup == "Any" || selectedGroup == T("Any"))
            return true;

        return groups is not null && groups.Contains(selectedGroup);
    }


    public static CustomFilter CreateSafeFilter(Func<GObject.Object, bool> predicate)
    {
        return CustomFilter.New(obj =>
        {
            try
            {
                return predicate(obj);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Shelly] FilterPackage threw, hiding row: {ex}");
                return false;
            }
        });
    }
}
