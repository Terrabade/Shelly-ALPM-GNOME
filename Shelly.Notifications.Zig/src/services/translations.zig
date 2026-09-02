const std = @import("std");

const c = @cImport({
    @cInclude("libintl.h");
    @cInclude("locale.h");
});

const DOMAIN = "shelly-notifications";

pub fn init() bool {
    _ = c.setlocale(c.LC_ALL, "");
    _ = c.bindtextdomain(DOMAIN, "/usr/share/locale");
    _ = c.textdomain(DOMAIN);
    return true;
}

pub fn _(msgid: [:0]const u8) [:0]const u8 {
    const result = c.gettext(msgid.ptr);
    return std.mem.span(result);
}
