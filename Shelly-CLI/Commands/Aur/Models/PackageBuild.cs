using MemoryPack;

namespace Shelly_CLI.Commands.Aur.Models;

[MemoryPackable]
public partial record PackageBuild(string Name, string? PkgBuild);