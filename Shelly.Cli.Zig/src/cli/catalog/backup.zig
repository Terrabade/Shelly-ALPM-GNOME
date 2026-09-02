const types = @import("types.zig");

const flag = types.flag;
const stringOption = types.stringOption;

pub const variants = [_]types.Variant{
    .{
        .action = .backup,
        .name = "utility",
        .default_for_action = true,
        .description = "Back up explicitly installed standard packages, AUR packages, and Flatpak applications as type-grouped TOML.",
        .options = &.{
            flag("--export", &.{"-e"}, "Exports a declaritive list of explicitly installed standard packages, AUR packages, and Flatpak applications"),
            flag("--import", &.{"-i"}, "Imports a previously exported backup and installs the latest versions of all items listed"),
            stringOption("--name", &.{"-a"}, "File name without the .toml extension", false),
            stringOption("--directory", &.{"-d"}, "Directory in which to write/read the backup", false),
        },
    },
};
