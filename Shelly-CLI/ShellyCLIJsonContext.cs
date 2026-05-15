using System.Text.Json.Serialization;
using PackageManager.AppImage;
using PackageManager.Alpm;
using PackageManager.Alpm.Pacfile;
using PackageManager.Aur.Models;
using PackageManager.Flatpak;
using PackageManager.Local;
using Shelly_CLI.Commands.Standard.Models;
using Shelly_CLI.Configuration;

namespace Shelly_CLI;

[JsonSerializable(typeof(List<AlpmPackageUpdateDto>))]
[JsonSerializable(typeof(AlpmPackageUpdateDto))]
[JsonSerializable(typeof(List<AlpmPackageDto>))]
[JsonSerializable(typeof(AlpmPackageDto))]
[JsonSerializable(typeof(List<LocalPackageDto>))]
[JsonSerializable(typeof(LocalPackageDto))]
[JsonSerializable(typeof(List<AurPackageDto>))]
[JsonSerializable(typeof(AurPackageDto))]
[JsonSerializable(typeof(List<AurUpdateDto>))]
[JsonSerializable(typeof(AurUpdateDto))]
[JsonSerializable(typeof(SyncModel))]
[JsonSerializable(typeof(SyncPackageModel))]
[JsonSerializable(typeof(SyncAurModel))]
[JsonSerializable(typeof(SyncFlatpakModel))]
[JsonSerializable(typeof(RssModel))]
[JsonSerializable(typeof(List<RssModel>))]
[JsonSerializable(typeof(List<AppImageDto>))]
[JsonSerializable(typeof(AppImageDto))]
[JsonSerializable(typeof(List<AppImageUpdateDto>))]
[JsonSerializable(typeof(AppImageUpdateDto))]
[JsonSerializable(typeof(ShellyConfig))]
[JsonSerializable(typeof(List<FlatpakPackageDto>))]
[JsonSerializable(typeof(FlatpakPackageDto))]
[JsonSerializable(typeof(List<FlatpakRemoteDto>))]
[JsonSerializable(typeof(FlatpakRemoteDto))]
[JsonSerializable(typeof(List<PacfileRecord>))]
[JsonSerializable(typeof(PacfileRecord))]
internal partial class ShellyCLIJsonContext : JsonSerializerContext;
