//! Stable user-facing presets for package archive compression. Keeping this
//! policy separate from libarchive lets configuration and writers share it.
const std = @import("std");

pub const Format = enum {
    zstd,
    gzip,
    xz,
    bzip2,

    pub fn fromPath(path: []const u8) !Format {
        inline for (.{ .{ ".zst", Format.zstd }, .{ ".gz", Format.gzip }, .{ ".xz", Format.xz }, .{ ".bz2", Format.bzip2 } }) |item| {
            if (std.mem.endsWith(u8, path, item[0])) return item[1];
        }
        return error.UnsupportedCompressionPresetFormat;
    }

    pub fn filterName(self: Format) [:0]const u8 {
        return switch (self) {
            .zstd => "zstd",
            .gzip => "gzip",
            .xz => "xz",
            .bzip2 => "bzip2",
        };
    }
};

pub const Preset = enum(u3) {
    conservative = 1,
    fast = 2,
    balanced = 3,
    compact = 4,
    maximum = 5,

    pub fn fromInt(value: i64) !Preset {
        return switch (value) {
            1 => .conservative,
            2 => .fast,
            3 => .balanced,
            4 => .compact,
            5 => .maximum,
            else => error.InvalidCompressionLevel,
        };
    }

    pub fn parse(value: []const u8) !Preset {
        if (value.len == 0) return error.InvalidCompressionLevel;
        for (value) |byte| if (!std.ascii.isDigit(byte)) return error.InvalidCompressionLevel;
        return fromInt(std.fmt.parseInt(i64, value, 10) catch return error.InvalidCompressionLevel);
    }

    pub fn backendLevel(self: Preset, format: Format) [:0]const u8 {
        const levels: [5][:0]const u8 = switch (format) {
            .zstd => .{ "1", "3", "6", "12", "19" },
            .gzip => .{ "1", "3", "6", "8", "9" },
            .xz => .{ "0", "3", "6", "7", "9" },
            .bzip2 => .{ "1", "3", "5", "7", "9" },
        };
        return levels[@intFromEnum(self) - 1];
    }
};
