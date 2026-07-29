const std = @import("std");
const Io = std.Io;
const Allocator = std.mem.Allocator;
const equalIgnoreCase = std.ascii.eqlIgnoreCase;

// Max depth is at most one for this configuration file.
// Why the hell would you ever want more than one of these???
const max_depth: usize = 1;

pub const MakePackageConfiguration = struct {
    const Self = @This();
    pub const default_path = "/etc/makepkg.conf";

    backing_allocator: Allocator,
    arena: std.heap.ArenaAllocator,

    // Starting with architecture flags as source acquisition isn't needed
    carch: []const u8 = "x86_64",
    chost: []const u8 = "x86_64-pc-linux-gnu",
    package_carch: []const u8 = "x86_64",
    nproc: u8 = 2,
    cpp_flags: []const u8 = "",
    c_flags: []const u8 = "-march=native -O3 -pipe -fno-plt -fexceptions -Wp,-D_FORTIFY_SOURCE=3 -Wformat -Werror=format-security -fstack-clash-protection -fcf-protection",
    cxx_flags: []const u8 = "-march=native -O3 -pipe -fno-plt -fexceptions -Wp,-D_FORTIFY_SOURCE=3 -Wformat -Werror=format-security -fstack-clash-protection -fcf-protection -Wp,-D_GLIBCXX_ASSERTIONS",
    ld_flags: []const u8 = "-Wl,-O1 -Wl,--sort-common -Wl,--as-needed -Wl,-z,relro -Wl,-z,now -Wl,-z,pack-relative-relocs",
    lto_flags: []const u8 = "-flto=auto",
    make_flags: []const u8 = "j2",
    ninja_flags: []const u8 = "j2",
    debug_c_flags: []const u8 = "-g",
    debug_cxx_flags: []const u8 = "-g",
    // Build environment flags
    //
    // Makepkg defaults: BUILDENV=(!distcc !color !ccache check !sign)
    //  A negated environment option will do the opposite of the comments below.
    //
    //-- distcc:   Use the Distributed C/C++/ObjC compiler
    //-- color:    Colorize output messages
    //-- ccache:   Use ccache to cache compilation
    //-- check:    Run the check() function if present in the PKGBUILD
    //-- sign:     Generate PGP signature file
    build_environment: []const u8 = "(!distcc color !ccache check !sign)",
    //-- If using DistCC, your MAKEFLAGS will also need modification. In addition,
    //-- specify a space-delimited list of hosts running in the DistCC cluster.
    distributed_c_compiler_hosts: []const u8 = "",
    build_directory: []const u8 = "/tmp/makepkg",
    // Global packaging options
    // Makepkg defaults:
    // OPTIONS=(!strip docs libtool staticlibs emptydirs !zipman !purge !debug !lto !autodeps)
    //  A negated option will do the opposite of the comments below.
    //-- strip:      Strip symbols from binaries/libraries
    //-- docs:       Save doc directories specified by DOC_DIRS
    //-- libtool:    Leave libtool (.la) files in packages
    //-- staticlibs: Leave static library (.a) files in packages
    //-- emptydirs:  Leave empty directories in packages
    //-- zipman:     Compress manual (man and info) pages in MAN_DIRS with gzip
    //-- purge:      Remove files specified by PURGE_TARGETS
    //-- debug:      Add debugging flags as specified in DEBUG_* variables
    //-- lto:        Add compile flags for building with link time optimization
    //-- autodeps:   Automatically add depends/provides
    options: []const u8 = "(strip docs !libtool !staticlibs emptydirs zipman purge !debug lto !autodeps)",
    // File integrity checks to use. Valid: md5, sha1, sha224, sha256, sha384, sha512, b2
    integrity_check: []const u8 = "(sha256)",
    strip_binaries: []const u8 = "--strip-all",
    strip_static: []const u8 = "--strip-unneeded",
    strip_shared: []const u8 = "--strip-debug",
    man_directories: []const u8 = "(usr{,/local}{,/share}/{man,info})",
    doc_directories: []const u8 = "(usr/{,local/}{,share/}{doc,gtk-doc})",
    purge_targets: []const u8 = "(usr/{,share}/info/dir .packlist *.pod)",
    debug_source_direcctory: []const u8 = "/usr/src/debug",
    library_directories: []const u8 = "('lib:usr/lib' 'lib32:usr/lib32')",
    // Package output options
    // Destination: specify a fixed directory where all packages will be placed
    package_destination: []const u8 = "/home/packages",
    // Source cache: specify a fixed directory where source files will be cached
    source_destination: []const u8 = "/home/sources",
    // Source packages: specify a fixed directory where all src packages will be placed
    source_package_destionation: []const u8 = "/home/srcpackages",
    // Log files: specify a fixed directory where all log files will be placed
    log_destination: []const u8 = "/home/makepkglogs",
    // Packager: name/email of the person or organization building packages
    packager: []const u8 = "Jane Doe <jane@doe.com>",
    // Specific gpg key to use for package signing
    gpg_key: []const u8 = "",
    // Compression defaults
    gz: []const u8 = "(gzip -c -f -n)",
    bz2: []const u8 = "(bzip2 -c -f)",
    xz: []const u8 = "(xz -c -z -)",
    zst: []const u8 = "(zstd -c -T0 -9 -)",
    lrz: []const u8 = "(lrzip -q)",
    lzo: []const u8 = "(lzop -q)",
    z: []const u8 = "(compress -c -f)",
    lz4: []const u8 = "(lz4 -q)",
    lz: []const u8 = "(lzip -c -f)",
    // Extension defaults
    package_extension: []const u8 = ".pkg.tar.zst",
    source_extension: []const u8 = "src.tar.gz",

    pub fn init(io: Io, allocator: Allocator) Allocator.Error!*Self {
        return initFromPath(io, allocator, default_path);
    }

    pub fn initFromPath(
        io: Io,
        allocator: Allocator,
        path: []const u8,
    ) Allocator.Error!*Self {
        const config = try createWithDefaults(allocator);
        errdefer config.deinit();

        var parser: Parser = .{
            .io = io,
            .scratch_allocator = allocator,
            .arena_allocator = config.arena.allocator(),
            .config = config,
        };
        try parser.parse_file(path);
        return config;
    }

    pub fn initFromBuffer(
        io: Io,
        allocator: Allocator,
        bytes: []const u8,
    ) Allocator.Error!*Self {
        const config = try createWithDefaults(allocator);
        errdefer config.deinit();

        var parser: Parser = .{
            .io = io,
            .scratch_allocator = allocator,
            .arena_allocator = config.arena.allocator(),
            .config = config,
        };
        try parser.parse_buffer(bytes);
        return config;
    }

    pub fn deinit(self: *Self) void {
        const allocator = self.backing_allocator;
        self.arena.deinit();
        allocator.destroy(self);
    }

    fn createWithDefaults(allocator: Allocator) Allocator.Error!*Self {
        const config = try allocator.create(Self);
        config.* = .{
            .backing_allocator = allocator,
            .arena = .init(allocator),
        };
        return config;
    }

    fn read_while_file(io: Io, alloc: Allocator, path: []const u8) ![]u8 {
        return Io.Dir.cwd().readFileAlloc(io, path, alloc, .unlimited);
    }

    const Parser = struct {
        io: Io,
        scratch_allocator: Allocator,
        arena_allocator: Allocator,
        config: *MakePackageConfiguration,
        depth: usize = 0,

        fn parse_file(self: *Parser, path: []const u8) Allocator.Error!void {
            if (self.depth >= max_depth) return;
            const bytes = read_while_file(self.io, self.scratch_allocator, path) catch |err| switch (err) {
                error.OutOfMemory => return error.OutOfMemory,
                else => return,
            };
            defer self.scratch_allocator.free(bytes);
            self.depth += 1;
            defer self.depth -= 1;
            try self.parse_buffer(bytes);
        }

        fn parse_buffer(self: *Parser, bytes: []const u8) Allocator.Error!void {
            var logical: std.ArrayList(u8) = .empty;
            defer logical.deinit(self.scratch_allocator);
            var next_line = false;

            var lines = std.mem.splitScalar(u8, bytes, '\n');
            while (lines.next()) |raw_input| {
                const raw = std.mem.trimEnd(u8, raw_input, " \r");
                const continues = hasLineContinuate(raw);

                const partial = if (next_line) std.mem.trimStart(u8, raw, " \t") else raw;

                if (continues) {
                    try logical.appendSlice(self.scratch_allocator, partial[0 .. partial.len - 1]);

                    if (logical.items.len > 0 and
                        logical.items[logical.items.len - 1] != ' ')
                    {
                        try logical.append(self.scratch_allocator, ' ');
                    }

                    next_line = true;
                    continue;
                }
                try logical.appendSlice(self.scratch_allocator, partial);

                const line = std.mem.trim(u8, logical.items, " \t\r\n");
                if (line.len != 0 and line[0] != '#') {
                    const eq_index = std.mem.indexOfScalar(u8, line, '=') orelse {
                        logical.clearRetainingCapacity();
                        next_line = false;
                        continue;
                    };
                    const owned_line = try self.arena_allocator.dupe(u8, line);

                    const key = std.mem.trim(u8, owned_line[0..eq_index], " \t");
                    var value = std.mem.trim(u8, owned_line[eq_index + 1 ..], " \t");

                    if (value.len >= 2 and
                        value[0] == '"' and
                        value[value.len - 1] == '"')
                    {
                        value = value[1 .. value.len - 1];
                    }
                    self.parse_key_value(key, value);
                }

                logical.clearRetainingCapacity();
                next_line = false;
            }
        }

        fn parse_key_value(self: *Parser, key: []const u8, value: []const u8) void {
            if (equalIgnoreCase(key, "carch")) {
                self.config.carch = value;
            } else if (equalIgnoreCase(key, "chost")) {
                self.config.chost = value;
            } else if (equalIgnoreCase(key, "package_carch")) {
                self.config.package_carch = value;
            } else if (equalIgnoreCase(key, "nproc")) {
                self.config.nproc = std.fmt.parseInt(u8, value, 10) catch return;
            } else if (equalIgnoreCase(key, "cppflags")) {
                self.config.cpp_flags = value;
            } else if (equalIgnoreCase(key, "cflags")) {
                self.config.c_flags = value;
            } else if (equalIgnoreCase(key, "cxxflags")) {
                self.config.cxx_flags = value;
            } else if (equalIgnoreCase(key, "ldflags")) {
                self.config.ld_flags = value;
            } else if (equalIgnoreCase(key, "ltoflags")) {
                self.config.lto_flags = value;
            } else if (equalIgnoreCase(key, "makeflags")) {
                self.config.make_flags = value;
            } else if (equalIgnoreCase(key, "ninjaflags")) {
                self.config.ninja_flags = value;
            } else if (equalIgnoreCase(key, "debug_cflags")) {
                self.config.debug_c_flags = value;
            } else if (equalIgnoreCase(key, "debug_cxxflags")) {
                self.config.debug_cxx_flags = value;
            } else if (equalIgnoreCase(key, "buildenv")) {
                self.config.build_environment = value;
            } else if (equalIgnoreCase(key, "distcc_hosts")) {
                self.config.distributed_c_compiler_hosts = value;
            } else if (equalIgnoreCase(key, "builddir")) {
                self.config.build_directory = value;
            } else if (equalIgnoreCase(key, "options")) {
                self.config.options = value;
            } else if (equalIgnoreCase(key, "integrity_check")) {
                self.config.integrity_check = value;
            } else if (equalIgnoreCase(key, "strip_binaries")) {
                self.config.strip_binaries = value;
            } else if (equalIgnoreCase(key, "strip_shared")) {
                self.config.strip_shared = value;
            } else if (equalIgnoreCase(key, "strip_static")) {
                self.config.strip_static = value;
            } else if (equalIgnoreCase(key, "man_dirs")) {
                self.config.man_directories = value;
            } else if (equalIgnoreCase(key, "doc_dirs")) {
                self.config.doc_directories = value;
            } else if (equalIgnoreCase(key, "purge_targets")) {
                self.config.purge_targets = value;
            } else if (equalIgnoreCase(key, "dbgsrcdir")) {
                self.config.debug_source_direcctory = value;
            } else if (equalIgnoreCase(key, "lib_dirs")) {
                self.config.library_directories = value;
            } else if (equalIgnoreCase(key, "pkgdest")) {
                self.config.package_destination = value;
            } else if (equalIgnoreCase(key, "srcdest")) {
                self.config.source_destination = value;
            } else if (equalIgnoreCase(key, "srcpkgdest")) {
                self.config.source_package_destionation = value;
            } else if (equalIgnoreCase(key, "logdest")) {
                self.config.log_destination = value;
            } else if (equalIgnoreCase(key, "packager")) {
                self.config.packager = value;
            } else if (equalIgnoreCase(key, "gpgkey")) {
                self.config.gpg_key = value;
            } else if (equalIgnoreCase(key, "compressgz")) {
                self.config.gz = value;
            } else if (equalIgnoreCase(key, "compressbz2")) {
                self.config.bz2 = value;
            } else if (equalIgnoreCase(key, "compressxz")) {
                self.config.xz = value;
            } else if (equalIgnoreCase(key, "compresszst")) {
                self.config.zst = value;
            } else if (equalIgnoreCase(key, "compresslrz")) {
                self.config.lrz = value;
            } else if (equalIgnoreCase(key, "compresslzo")) {
                self.config.lzo = value;
            } else if (equalIgnoreCase(key, "compressz")) {
                self.config.z = value;
            } else if (equalIgnoreCase(key, "compresslz4")) {
                self.config.lz4 = value;
            } else if (equalIgnoreCase(key, "compresslz")) {
                self.config.lz = value;
            } else if (equalIgnoreCase(key, "pkgext")) {
                self.config.package_extension = value;
            } else if (equalIgnoreCase(key, "srcext")) {
                self.config.source_extension = value;
            }
        }

        fn hasLineContinuate(raw: []const u8) bool {
            const line = std.mem.trimEnd(u8, raw, "\r");
            if (line.len == 0 or line[line.len - 1] != '\\') return false;

            var slash_count: usize = 0;
            var index = line.len;
            while (index > 0 and line[index - 1] == '\\') {
                slash_count += 1;
                index -= 1;
            }

            return slash_count % 2 == 1;
        }
    };
};

fn TestParser(comptime assertion: *const fn (*const MakePackageConfiguration) anyerror!void) type {
    return struct {
        fn run(input: []const u8) !void {
            const config = try MakePackageConfiguration.initFromBuffer(
                std.testing.io,
                std.testing.allocator,
                input,
            );
            defer config.deinit();
            try assertion(config);
        }
    };
}

test "makepkg parser reads a quoted scalar case-insensitively" {
    const Assert = struct {
        fn value(config: *const MakePackageConfiguration) !void {
            try std.testing.expectEqualStrings("x86_64-pc-linux-gnu", config.chost);
        }
    };
    try TestParser(&Assert.value).run("cHoSt=\"x86_64-pc-linux-gnu\"\n");
}

test "makepkg parser joins backslash-continued CFLAGS" {
    const Assert = struct {
        fn value(config: *const MakePackageConfiguration) !void {
            try std.testing.expectEqualStrings(
                "-march=native -O3 -pipe -fno-plt -fexceptions " ++
                    "-Wp,-D_FORTIFY_SOURCE=3 -Wformat -Werror=format-security " ++
                    "-fstack-clash-protection -fcf-protection",
                config.c_flags,
            );
        }
    };
    const input =
        \\CFLAGS="-march=native -O3 -pipe -fno-plt -fexceptions \
        \\        -Wp,-D_FORTIFY_SOURCE=3 -Wformat -Werror=format-security \
        \\        -fstack-clash-protection -fcf-protection"
    ;
    try TestParser(&Assert.value).run(input);
}

test "makepkg parser retains parenthesized option values" {
    const Assert = struct {
        fn value(config: *const MakePackageConfiguration) !void {
            try std.testing.expectEqualStrings(
                "(!strip docs !libtool staticlibs)",
                config.options,
            );
        }
    };
    try TestParser(&Assert.value).run(
        "OPTIONS=(!strip docs !libtool staticlibs)\n",
    );
}

test "makepkg parser ignores comments unknown keys and lines without assignments" {
    const Assert = struct {
        fn value(config: *const MakePackageConfiguration) !void {
            try std.testing.expectEqualStrings("-j8", config.make_flags);
            try std.testing.expectEqualStrings("x86_64-pc-linux-gnu", config.chost);
            try std.testing.expectEqual(@as(u8, 2), config.nproc);
        }
    };
    try TestParser(&Assert.value).run(
        "# MAKEFLAGS=-j1\n" ++
            "UNKNOWN=value\n" ++
            "not-an-assignment\n" ++
            "MAKEFLAGS=\"-j8\"\n",
    );
}

test "makepkg initialization parses a file over declared defaults" {
    var temporary = std.testing.tmpDir(.{});
    defer temporary.cleanup();
    try temporary.dir.writeFile(std.testing.io, .{
        .sub_path = "makepkg.conf",
        .data = "CARCH=\"aarch64\"\nNPROC=12\n",
    });
    const path = try temporary.dir.realPathFileAlloc(
        std.testing.io,
        "makepkg.conf",
        std.testing.allocator,
    );
    defer std.testing.allocator.free(path);

    const config = try MakePackageConfiguration.initFromPath(
        std.testing.io,
        std.testing.allocator,
        path,
    );
    defer config.deinit();

    try std.testing.expectEqualStrings("aarch64", config.carch);
    try std.testing.expectEqual(@as(u8, 12), config.nproc);
    try std.testing.expectEqualStrings("x86_64-pc-linux-gnu", config.chost);
}

test "makepkg parser parses NPROC as an integer" {
    const Assert = struct {
        fn value(config: *const MakePackageConfiguration) !void {
            try std.testing.expectEqual(@as(u8, 16), config.nproc);
        }
    };
    try TestParser(&Assert.value).run("NPROC=16\n");
}

test "makepkg continuation requires an odd trailing backslash count" {
    const Parser = MakePackageConfiguration.Parser;
    try std.testing.expect(Parser.hasLineContinuate("CFLAGS=\"-O2 \\"));
    try std.testing.expect(!Parser.hasLineContinuate("CFLAGS=\"-O2 \\\\"));
    try std.testing.expect(Parser.hasLineContinuate("CFLAGS=\"-O2 \\\\\\"));
    try std.testing.expect(!Parser.hasLineContinuate("CFLAGS=\"-O2\""));
    try std.testing.expect(Parser.hasLineContinuate("CFLAGS=\"-O2 \\\r"));
}
