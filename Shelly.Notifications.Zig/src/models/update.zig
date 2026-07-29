const std = @import("std");

pub const CheckUpdates = struct {
    Packages: []CheckUpdatesPackage = &.{},
    Aur: []CheckUpdatesAur = &.{},
    Flatpak: []CheckUpdatesFlatpak = &.{},

    pub fn count(self: *const CheckUpdates) usize {
        return self.Packages.len + self.Aur.len + self.Flatpak.len;
    }
};

pub const CheckUpdatesPackage = struct {
    Name: []const u8 = "",
    CurrentVersion: []const u8 = "",
    NewVersion: []const u8 = "",
};

pub const CheckUpdatesAur = struct {
    Name: []const u8 = "",
    Version: []const u8 = "",
    NewVersion: []const u8 = "",
};

pub const CheckUpdatesFlatpak = struct {
    Name: []const u8 = "",
    Version: []const u8 = "",
};
