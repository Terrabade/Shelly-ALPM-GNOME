const std = @import("std");
const bindings = @import("Shelly_Ui_Gtk");
const gtk = bindings.gtk;
const gio = bindings.gio;
const gobject = bindings.gobject;
const c_string = @import("../../helpers/c_string.zig");
const support = @import("../../pages/support.zig");
const Option = @import("../../services/shelly_operation.zig").Option;

pub const ProviderDialog = extern struct {
    parent_instance: Parent,
    const Self = @This();
    pub const Parent = gtk.Box;
    const resource_path = "/com/shellyorg/shelly/dialog/ui/provider.ui";

    pub const ResponseFn = *const fn (ctx: ?*anyopaque, confirmed: bool, selected: usize) void;

    const Private = struct {
        title_label: *gtk.Label,
        dropdown: *gtk.DropDown,
        confirm_button: *gtk.Button,
        cancel_button: *gtk.Button,
        on_response: ?ResponseFn,
        ctx: ?*anyopaque,
        indices: []usize,
        indices_len: usize,
        var offset: c_int = 0;
    };

    pub const getGObjectType = gobject.ext.defineClass(Self, .{
        .name = "ShellySingleProviderDialog",
        .instanceInit = &init,
        .classInit = &Class.init,
        .parent_class = &Class.parent,
        .private = .{ .Type = Private, .offset = &Private.offset },
    });

    fn priv(self: *Self) *Private {
        return gobject.ext.impl_helpers.getPrivate(self, Private, Private.offset);
    }

    pub fn as(self: *Self, comptime T: type) *T {
        return gobject.ext.as(T, self);
    }

    fn init(self: *Self, _: *Class) callconv(.c) void {
        gtk.Widget.initTemplate(self.as(gtk.Widget));
        const p = self.priv();
        p.on_response = null;
        p.ctx = null;
        p.indices = &.{};
        p.indices_len = 0;
    }

    pub fn new(
        alloc: std.mem.Allocator,
        title: []const u8,
        options: []const Option,
        on_response: ResponseFn,
        ctx: ?*anyopaque,
    ) *Self {
        const self = gobject.ext.newInstance(Self, .{});
        const p = self.priv();

        var tbuf: [512]u8 = undefined;
        gtk.Label.setLabel(p.title_label, c_string.cstr(&tbuf, title));
        p.on_response = on_response;
        p.ctx = ctx;

        p.indices = alloc.alloc(usize, options.len) catch &.{};
        p.indices_len = if (p.indices.len == options.len) options.len else 0;

        const strings = gtk.StringList.new(null);
        var preselect: u32 = 0;
        for (options, 0..) |opt, i| {
            var nbuf: [256]u8 = undefined;
            gtk.StringList.append(strings, c_string.cstr(&nbuf, opt.name));
            if (p.indices_len > 0) p.indices[i] = opt.index;

            if ((opt.is_selected or opt.is_installed) and preselect == 0) {
                preselect = @intCast(i);
            }
        }
        gtk.DropDown.setModel(p.dropdown, strings.as(gio.ListModel));
        strings.as(gobject.Object).unref();
        gtk.DropDown.setSelected(p.dropdown, preselect);

        return self;
    }

    fn on_confirm(self: *Self) callconv(.c) void {
        const p = self.priv();
        const sel = gtk.DropDown.getSelected(p.dropdown);
        if (sel == std.math.maxInt(u32) or sel >= p.indices_len) {
            if (p.on_response) |cb| cb(p.ctx, false, 0);
            return;
        }
        if (p.on_response) |cb| cb(p.ctx, true, p.indices[sel]);
    }

    fn on_cancel(self: *Self) callconv(.c) void {
        const p = self.priv();
        if (p.on_response) |cb| cb(p.ctx, false, 0);
    }

    const template_children = .{
        .{ "title_label", @offsetOf(Private, "title_label") },
        .{ "dropdown", @offsetOf(Private, "dropdown") },
        .{ "confirm_button", @offsetOf(Private, "confirm_button") },
        .{ "cancel_button", @offsetOf(Private, "cancel_button") },
    };

    pub const Class = extern struct {
        parent_class: Parent.Class,
        var parent: *Parent.Class = undefined;
        pub const Instance = Self;
        fn init(class: *Class) callconv(.c) void {
            const wc = gobject.ext.as(gtk.Widget.Class, class);
            gtk.Widget.Class.setTemplateFromResource(wc, resource_path);
            inline for (template_children) |c| {
                support.bindChild(class, Private.offset, c[0], c[1]);
            }
            gtk.Widget.Class.bindTemplateCallbackFull(wc, "on_confirm", @ptrCast(&on_confirm));
            gtk.Widget.Class.bindTemplateCallbackFull(wc, "on_cancel", @ptrCast(&on_cancel));
        }
    };
};
