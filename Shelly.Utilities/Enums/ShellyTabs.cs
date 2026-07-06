using System.Text.Json.Serialization;

namespace Shelly.Utilities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ShellyTabs>))]
public enum ShellyTabs
{
    Packages = 0,
    Aur = 1,
    Flatpak = 2,
    AppImage = 3,
    ShellySearch = 4,
    Recommend = 5
}