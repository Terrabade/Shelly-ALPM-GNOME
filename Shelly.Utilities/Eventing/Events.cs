using System;


namespace Shelly.Utilities.Eventing;

public abstract record Event(EventSource Source, EventLevel Level, DateTimeOffset TimeStamp = default)
{
    public DateTimeOffset TimeStamp { get; init; } = TimeStamp == default ? DateTimeOffset.Now : TimeStamp;
}

public sealed record AlpmErrorEvent(EventLevel Level, string ErrorMessage, DateTimeOffset TimeStamp = default)
    : Event(EventSource.Alpm, Level, TimeStamp);

public sealed record AlpmHookEvent(EventLevel Level, string Description, DateTimeOffset TimeStamp = default)
    : Event(EventSource.Alpm, Level, TimeStamp);

public sealed record AlpmScriptletEvent(EventLevel Level, string Line, DateTimeOffset TimeStamp = default)
    : Event(EventSource.Alpm, Level, TimeStamp);

public sealed record AlpmReplaceEvent(
    string Repository,
    string PackageName,
    List<string> Replaces,
    DateTimeOffset TimeStamp = default) : Event(EventSource.Alpm, EventLevel.Information, TimeStamp);

public abstract record ProgressEvent(
    EventSource Source,
    string ProgressType,
    int Percent,
    DateTimeOffset TimeStamp = default)
    : Event(Source, EventLevel.Information, TimeStamp);

public sealed record AlpmPackageProgresEvent(
    string PackageName,
    int CurrentIndex,
    int TotalPackages,
    string Status,
    string ProgressType,
    int Percent,
    string? Message,
    DateTimeOffset TimeStamp = default)
    : ProgressEvent(EventSource.Alpm, ProgressType, Percent, TimeStamp);