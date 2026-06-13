using Spectre.Console.Cli;

namespace Shelly_CLI.Commands.Standard;

public class PurifyPackagesSettings : CommandSettings
{
    [CommandOption("-n|--no-confirm")]
    public bool NoConfirm { get; set; }

    [CommandOption("-d|--dry-run")]
    public bool DryRun { get; set; }

    [CommandOption("-o|--orphans")]
    public bool Orphans { get; set; }
}