const std = @import("std");
const Profile = enum { pacman, devario };

pub fn build(b: *std.Build) void {
    const target = b.standardTargetOptions(.{});
    const optimize = b.standardOptimizeOption(.{});
    const profile = b.option(Profile, "path-profile", "Distribution path defaults (independent of backend)") orelse .pacman;
    const options = b.addOptions();
    options.addOption(bool, "devario", profile == .devario);
    const mod = b.addModule("paths", .{
        .root_source_file = b.path("src/root.zig"),
        .target = target,
        .optimize = optimize,
    });
    mod.addOptions("path_options", options);
    const tests = b.addTest(.{ .root_module = mod });
    b.step("test", "Test distribution path defaults").dependOn(&b.addRunArtifact(tests).step);
}
