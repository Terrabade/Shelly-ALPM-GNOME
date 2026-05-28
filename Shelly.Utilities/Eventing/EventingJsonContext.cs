using System.Text.Json.Serialization;

namespace Shelly.Utilities.Eventing;

[JsonSourceGenerationOptions(
    MaxDepth = 256,
    GenerationMode = JsonSourceGenerationMode.Default,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(AlpmErrorEvent))]
[JsonSerializable(typeof(AlpmHookEvent))]
[JsonSerializable(typeof(AlpmScriptletEvent))]
[JsonSerializable(typeof(AlpmReplaceEvent))]
[JsonSerializable(typeof(AlpmPackageProgressEvent))]
[JsonSerializable(typeof(AlpmPkgBuildDiffEvent))]
[JsonSerializable(typeof(AlpmStatusEvent))]
[JsonSerializable(typeof(AlpmInformationalEvent))]
[JsonSerializable(typeof(AlpmEvents))]
[JsonSerializable(typeof(EventSource))]
[JsonSerializable(typeof(EventLevel))]
[JsonSerializable(typeof(Event))]
[JsonSerializable(typeof(List<string>))]
public partial class EventingJsonContext : JsonSerializerContext;