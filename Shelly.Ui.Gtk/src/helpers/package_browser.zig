const std = @import("std");
const RecommendCategory = @import("../models/recommendation.zig").RecommendCategory;

pub const SidebarMode = enum(u8) {
    categories = 0,
    groups = 1,
    repositories = 2,
};

pub fn sidebarModeAllowed(mode: SidebarMode, software_mode: bool) bool {
    return !software_mode or mode == .categories;
}

pub fn matchesCategory(categories: []const RecommendCategory, category_name: []const u8, package_name: []const u8) bool {
    for (categories) |category| {
        if (!std.mem.eql(u8, category.name, category_name)) continue;
        for (category.packages) |name| {
            if (std.mem.eql(u8, name, package_name)) return true;
        }
    }
    return false;
}

pub fn sortStrings(values: [][]const u8) void {
    var i: usize = 1;
    while (i < values.len) : (i += 1) {
        const value = values[i];
        var j = i;
        while (j > 0 and std.mem.lessThan(u8, value, values[j - 1])) : (j -= 1) {
            values[j] = values[j - 1];
        }
        values[j] = value;
    }
}

test "category membership matches exact recommended package names" {
    const audio_packages = [_][]const u8{ "pipewire", "easyeffects" };
    const browser_packages = [_][]const u8{"firefox"};
    const categories = [_]RecommendCategory{
        .{ .name = "Audio", .packages = &audio_packages },
        .{ .name = "Browsers", .packages = &browser_packages },
    };

    try std.testing.expect(matchesCategory(&categories, "Audio", "pipewire"));
    try std.testing.expect(!matchesCategory(&categories, "Audio", "firefox"));
    try std.testing.expect(!matchesCategory(&categories, "Audio", "pipe"));
    try std.testing.expect(!matchesCategory(&categories, "Missing", "pipewire"));
}

test "software mode only allows category browsing" {
    try std.testing.expect(sidebarModeAllowed(.categories, true));
    try std.testing.expect(!sidebarModeAllowed(.groups, true));
    try std.testing.expect(!sidebarModeAllowed(.repositories, true));
    try std.testing.expect(sidebarModeAllowed(.categories, false));
    try std.testing.expect(sidebarModeAllowed(.groups, false));
    try std.testing.expect(sidebarModeAllowed(.repositories, false));
}

test "sidebar values use locale independent lexical ordering" {
    var values = [_][]const u8{ "zsh", "base", "core" };
    sortStrings(&values);
    try std.testing.expectEqualStrings("base", values[0]);
    try std.testing.expectEqualStrings("core", values[1]);
    try std.testing.expectEqualStrings("zsh", values[2]);
}
