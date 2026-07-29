const std = @import("std");

pub const domain = "shelly-ui";
pub const locale_dir = "/usr/share/locale";

const LC_ALL: c_int = 6;

extern fn setlocale(category: c_int, locale: [*:0]const u8) ?[*:0]const u8;
extern fn bindtextdomain(domainname: [*:0]const u8, dirname: [*:0]const u8) ?[*:0]const u8;
extern fn bind_textdomain_codeset(domainname: [*:0]const u8, codeset: [*:0]const u8) ?[*:0]const u8;
extern fn textdomain(domainname: [*:0]const u8) ?[*:0]const u8;
extern fn dgettext(domainname: [*:0]const u8, msgid: [*:0]const u8) [*:0]const u8;

/// Initializes the process locale and the gettext domain used by both Zig
/// strings and GtkBuilder resources.
pub fn init() bool {
    const locale_ok = setlocale(LC_ALL, "") != null;
    const directory_ok = bindtextdomain(domain, locale_dir) != null;
    const codeset_ok = bind_textdomain_codeset(domain, "UTF-8") != null;
    const domain_ok = textdomain(domain) != null;
    return locale_ok and directory_ok and codeset_ok and domain_ok;
}

pub fn _(msgid: [:0]const u8) [:0]const u8 {
    return std.mem.span(dgettext(domain, msgid.ptr));
}
