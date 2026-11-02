const std = @import("std");

pub const domain = "shelly-ui";
const fallback_locale_dir = "/usr/share/locale";

const LC_ALL: c_int = 6;

extern "c" fn readlink(path: [*:0]const u8, buf: [*]u8, bufsize: usize) isize;
extern "c" fn access(path: [*:0]const u8, mode: c_int) c_int;

const F_OK: c_int = 0;

extern fn setlocale(category: c_int, locale: [*:0]const u8) ?[*:0]const u8;
extern fn bindtextdomain(domainname: [*:0]const u8, dirname: [*:0]const u8) ?[*:0]const u8;
extern fn bind_textdomain_codeset(domainname: [*:0]const u8, codeset: [*:0]const u8) ?[*:0]const u8;
extern fn textdomain(domainname: [*:0]const u8) ?[*:0]const u8;
extern fn dgettext(domainname: [*:0]const u8, msgid: [*:0]const u8) [*:0]const u8;

pub fn init() bool {
    return initWithLocale(null);
}

extern fn setenv(name: [*:0]const u8, value: [*:0]const u8, overwrite: c_int) c_int;

var locale_dir_buffer: [std.fs.max_path_bytes]u8 = undefined;
var locale_dir_result: ?[:0]const u8 = null;

pub fn localeDirForExecutable(executable: []const u8, buffer: []u8) ?[:0]const u8 {
    const bin_marker = "/bin/";
    const prefix_end = std.mem.lastIndexOf(u8, executable, bin_marker) orelse return null;
    return std.fmt.bufPrintZ(buffer, "{s}/share/locale", .{executable[0..prefix_end]}) catch null;
}

pub fn localeDir() [:0]const u8 {
    if (locale_dir_result) |dir| return dir;

    var executable_buffer: [std.fs.max_path_bytes]u8 = undefined;
    const length = readlink("/proc/self/exe", &executable_buffer, executable_buffer.len);
    if (length > 0 and length < executable_buffer.len) {
        const executable = executable_buffer[0..@intCast(length)];
        if (localeDirForExecutable(executable, &locale_dir_buffer)) |dir| {
            if (access(dir.ptr, F_OK) == 0) locale_dir_result = dir;
        }
    }
    return locale_dir_result orelse fallback_locale_dir;
}

pub fn initWithLocale(override_locale: ?[:0]const u8) bool {
    _ = setlocale(LC_ALL, "");

    if (override_locale) |loc| {
        _ = setenv("LANGUAGE", loc.ptr, 1);
    }

    const directory_ok = bindtextdomain(domain, localeDir()) != null;
    const codeset_ok = bind_textdomain_codeset(domain, "UTF-8") != null;
    const domain_ok = textdomain(domain) != null;
    return directory_ok and codeset_ok and domain_ok;
}

pub fn _(msgid: [:0]const u8) [:0]const u8 {
    return std.mem.span(dgettext(domain, msgid.ptr));
}

test "locale directory follows the executable prefix" {
    var buffer: [std.fs.max_path_bytes]u8 = undefined;

    const local_dir = localeDirForExecutable("/home/example/.local/bin/shelly-ui", &buffer);
    try std.testing.expectEqualStrings("/home/example/.local/share/locale", local_dir.?);

    const system_dir = localeDirForExecutable("/usr/bin/shelly-ui", &buffer);
    try std.testing.expectEqualStrings("/usr/share/locale", system_dir.?);

    try std.testing.expectEqual(@as(?[:0]const u8, null), localeDirForExecutable("/shelly-ui", &buffer));
}
