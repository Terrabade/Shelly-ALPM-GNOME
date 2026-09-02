const types = @import("types.zig");

const flag = types.flag;

pub const variants = [_]types.Variant{
    .{
        .action = .news,
        .name = "standard",
        .default_for_action = true,
        .description = "Fetch Arch Linux news, display unread entries by default, and remember viewed entries in the invoking user's XDG cache.",
        .options = &.{flag(
            "--all",
            &.{"-a"},
            "Show previously viewed news as well as unread entries",
        )},
    },
};
