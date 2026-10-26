//! Distribution defaults only. Configuration and explicit API options override these.
const devario = @import("path_options").devario;
pub const profile = if (devario) "devario" else "pacman";
pub const config_file = if (devario) "/etc/shelly.conf" else "/etc/pacman.conf";
pub const config_directory = if (devario) "/etc/shelly.d" else "/etc/pacman.d";
pub const database = if (devario) "/var/lib/shelly" else "/var/lib/pacman";
pub const cache = if (devario) "/var/cache/shelly/pkg" else "/var/cache/pacman/pkg";
pub const keyring = config_directory ++ "/gnupg";
pub const keyring_bundles = if (devario) "/usr/share/shelly/keyrings" else "/usr/share/pacman/keyrings";
pub const mirrorlist = config_directory ++ "/mirrorlist";
pub const log = "/var/log/shelly.log";
// Preserve the legacy bootstrap log for the default profile.
pub const bootstrap_log = if (devario) log else "/var/log/pacman.log";
pub const system_hooks = if (devario) "/usr/share/rlpm/hooks" else "/usr/share/libalpm/hooks";
pub const admin_hooks = config_directory ++ "/hooks";
pub const hook_directories = [_][:0]const u8{ system_hooks, admin_hooks };

test "profile defaults keep administrator hooks after system hooks" {
    const std = @import("std");
    try std.testing.expectEqualStrings(admin_hooks, hook_directories[1]);
    try std.testing.expectEqualStrings(if (devario) "/etc/shelly.conf" else "/etc/pacman.conf", config_file);
    try std.testing.expectEqualStrings(if (devario) "/var/lib/shelly" else "/var/lib/pacman", database);
}
