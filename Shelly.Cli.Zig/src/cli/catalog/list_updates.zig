const types = @import("types.zig");

const flag = types.flag;

pub const variants = [_]types.Variant{
    .{
        .action = .list_updates,
        .name = "all",
        .type_code = 'x',
        .bare_action_code = true,
        .description = "Query available updates from every supported package backend, continuing through independent backend failures.",
        .implementation = "Combined Zig coordinator over AlpmManager, appimage.UpdateManager, AurManager, and FlatpakManager",
        .options = &.{ flag("--show-hidden", &.{}, "Include hidden packages"), flag("--no-devel", &.{}, "Does not check for -git builds") },
    },
    .{
        .action = .list_updates,
        .name = "standard",
        .type_code = 's',
        .description = "List available standard repository package updates.",
        .implementation = "Zigalpm.AlpmManager.sync_for_update_check / get_updates_available",
    },
    .{
        .action = .list_updates,
        .name = "appimage",
        .type_code = 'i',
        .description = "List installed AppImages with available updates.",
        .implementation = "Zigalpm.appimage.UpdateManager.get_updates",
    },
    .{
        .action = .list_updates,
        .name = "aur",
        .type_code = 'a',
        .description = "List installed AUR packages with available updates.",
        .implementation = "Zigalpm.AurManager.getPackagesNeedingUpdate",
        .options = &.{ flag("--show-hidden", &.{}, "Include hidden packages"), flag("--no-devel", &.{}, "Does not check for -git builds") },
    },
    .{
        .action = .list_updates,
        .name = "flatpak",
        .type_code = 'f',
        .description = "List Flatpak applications and runtimes with available updates.",
        .implementation = "Zigalpm.FlatpakManager.get_updates_flatpak",
    },
};
