using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Shelly.Utilities.Eventing;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(PkgbuildDiffQuestionDto), "q.pkgbuilddiff")]
public abstract record QuestionRequest(string QuestionId);

public sealed record PkgbuildDiffQuestionDto(
    string QuestionId,
    string PackageName,
    string? OldPkgbuild,
    string NewPkgbuild,
    List<PkgbuildWarningDto>? Warnings,
    List<string>? DiffLines = null) : QuestionRequest(QuestionId);

public sealed record PkgbuildWarningDto(
    string Tool,
    string Severity,
    string Hook,
    string MatchedLine,
    string Message);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
[JsonDerivedType(typeof(PkgbuildDiffAnswer), "a.pkgbuilddiff")]
public abstract record QuestionResponseDto(string QuestionId);

public sealed record PkgbuildDiffAnswer(
    string QuestionId,
    bool ProceedWithUpdate) : QuestionResponseDto(QuestionId);
