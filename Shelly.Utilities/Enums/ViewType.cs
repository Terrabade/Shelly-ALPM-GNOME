using System.Text.Json.Serialization;

namespace Shelly.Utilities.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ViewType>))]
public enum ViewType
{
    Grid = 0,
    List = 1
}