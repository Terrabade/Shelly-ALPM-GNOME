const std = @import("std");
const bindings = @import("Shelly_Ui_Gtk");
const gtk = bindings.gtk;
const gobject = bindings.gobject;
const support = @import("../../pages/support.zig");

pub const PolkitDialog = extern struct {
    parent_instance: Parent,
    const Self = @This();
    pub const Parent = gtk.Box;
    const resource_path = "/com/shellyorg/shelly/dialog/ui/polkit_warning.ui";
    pub const CloseFn = *const fn (ctx: ?*anyopaque) void;

    const Private = struct {
        title_label: *gtk.Label,
        message_label: *gtk.Label,
        close_button: *gtk.Button,
        on_close: ?CloseFn,
        ctx: ?*anyopaque,
        var offset: c_int = 0;
    };

    pub const getGObjectType = gobject.ext.defineClass(Self, .{
        .name = "ShellyPolkitDialog",
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
        p.on_close = null;
        p.ctx = null;
        _ = gobject.signalConnectData(
            p.message_label.as(gobject.Object),
            "activate-link",
            @ptrCast(&on_activate_link),
            null,
            null,
            .{},
        );
    }

    pub fn new(on_close: CloseFn, ctx: ?*anyopaque) *Self {
        const self = gobject.ext.newInstance(Self, .{});
        const p = self.priv();
        p.on_close = on_close;
        p.ctx = ctx;
        return self;
    }

    pub fn focusClose(self: *Self) void {
        const p = self.priv();
        _ = gtk.Widget.grabFocus(p.close_button.as(gtk.Widget));
    }

    fn on_close_btn(self: *Self) callconv(.c) void {
        const p = self.priv();
        if (p.on_close) |cb| cb(p.ctx);
    }

    fn on_activate_link(label: *gtk.Label, uri: [*:0]const u8, _: ?*anyopaque) callconv(.c) c_int {
        const root = gtk.Widget.getRoot(label.as(gtk.Widget));
        const parent: ?*gtk.Window = if (root) |r| gobject.ext.cast(gtk.Window, r) else null;
        const launcher = gtk.UriLauncher.new(uri);
        launcher.launch(parent, null, null, null);
        return 1;
    }

    const template_children = .{
        .{ "title_label", @offsetOf(Private, "title_label") },
        .{ "message_label", @offsetOf(Private, "message_label") },
        .{ "close_button", @offsetOf(Private, "close_button") },
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
            gtk.Widget.Class.bindTemplateCallbackFull(wc, "on_close", @ptrCast(&on_close_btn));
        }
    };
};
