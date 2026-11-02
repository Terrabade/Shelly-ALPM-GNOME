const std = @import("std");
const diagnostics = @import("diagnostics");
const bindings = @import("Shelly_Ui_Gtk");
const gtk = bindings.gtk;
const adw = bindings.adw;
const gio = bindings.gio;
const glib = bindings.glib;
const gobject = bindings.gobject;
const features = @import("services/build_features.zig");

const FlatpakPage = if (features.builtWithFlatpak) @import("pages/flatpak/flatpak_page.zig").FlatpakPage else void;
const FlatpakSlot = if (features.builtWithFlatpak) *FlatpakPage else void;
const AppImagePage = if (features.builtWithAppImage) @import("pages/appimage_page.zig").AppImagePage else void;
const AppImageSlot = if (features.builtWithAppImage) *AppImagePage else void;
const PackagePage = if (features.builtWithDevario)
    @import("devario/package_page.zig").PackagePage
else
    @import("pages/package_page.zig").PackagePage;
const AurPage = if (features.builtWithBuiltinAur) @import("pages/aur_page.zig").AurPage else void;
const AtollAurPage = if (features.builtWithAtoll) @import("pages/atoll_aur_page.zig").AtollAurPage else void;
const aur_icon_name: [:0]const u8 = if (features.builtWithAtollOnly) "atoll-shark-symbolic" else if (features.builtWithBuiltinAur) AurPage.icon_name else AtollAurPage.icon_name;
const aur_title: [:0]const u8 = features.aur_source_name;
const ShellySearchPage = if (features.builtWithShellySearch) @import("pages/search_page.zig").ShellySearchPage else void;
const UpdatePage = @import("pages/update_page.zig").UpdatePage;
const RecommendPage = if (features.builtWithRecommend) @import("pages/recommend_page.zig").RecommendPage else void;
const RecommendSlot = if (features.builtWithRecommend) *RecommendPage else void;
const WelcomePage = @import("pages/welcome.zig").WelcomePage;
const SupportPage = @import("pages/support.zig");
const SettingsPage = @import("pages/settings_page.zig").SettingsPage;
const PackageDetail = @import("pages/package_detail.zig").PackageDetail;
const UtilitiesPage = @import("pages/utilities_page.zig").UtilitiesPage;
const TransactionPage = @import("pages/transaction_page.zig").TransactionPage;
const TransactionRequest = @import("pages/transaction_page.zig").TransactionRequest;
const runtime = @import("services/runtime.zig");
const ShellyCli = @import("services/shelly_cli.zig").ShellyCli;
const AtollApiService = @import("services/atoll_api.zig").AtollApiService;
const NavMode = @import("models/shelly_config.zig").NavMode;
const translations = @import("helpers/translations.zig");
const a11y = @import("helpers/a11y.zig");
const deep_link = @import("helpers/deep_link.zig");
const ConfirmDialog = @import("dialog/page/yn_dialog.zig").ConfirmDialog;
const PolkitDialog = @import("dialog/page/polkit_warning.zig").PolkitDialog;
const DBus = @import("services/dbus.zig").DBus;

const NavButton = struct {
    button: *gtk.Button,
    name: [:0]const u8,
    window: *ShellyWindow,
};

pub const ShellyWindow = extern struct {
    parent_instance: Parent,

    pub const Parent = adw.ApplicationWindow;

    const default_aur_base = "https://aur.archlinux.org";

    const Private = struct {
        header_bar: *adw.HeaderBar,
        view_switcher: *adw.ViewSwitcher,
        content_stack: *gtk.Stack,
        primary_stack: *adw.ViewStack,
        explore_page: *PackagePage,
        installed_page: *PackagePage,
        recommend_page: RecommendSlot,
        lockout_overlay: *gtk.Box,
        lockout_content: *gtk.Box,
        full_detail_overlay: *gtk.Revealer,
        full_detail_content: *gtk.Box,
        full_detail_page: *PackageDetail,
        header_back_button: *gtk.Button,
        header_search_button: *gtk.Button,
        settings_page: *SettingsPage,
        settings_dialog: *adw.Dialog,
        utilities_page: *UtilitiesPage,
        utilities_dialog: *adw.Dialog,
        aur_button: *gtk.Button,
        flatpak_button: *gtk.Button,
        appimage_button: *gtk.Button,
        search_button: *gtk.Button,
        utilities_button: *gtk.Button,
        settings_button: *gtk.Button,
        about_button: *gtk.Button,
        nav_buttons: std.ArrayListUnmanaged(*NavButton),
        flatpak_page: FlatpakSlot,
        appimage_page: AppImageSlot,
        aur_slot_atoll: bool,
        var offset: c_int = 0;
    };

    pub const getGObjectType = gobject.ext.defineClass(ShellyWindow, .{
        .name = "ShellyWindow",
        .instanceInit = &init,
        .classInit = &Class.init,
        .parent_class = &Class.parent,
        .private = .{ .Type = Private, .offset = &Private.offset },
    });

    pub fn new(app: *gtk.Application) *ShellyWindow {
        return gobject.ext.newInstance(ShellyWindow, .{ .application = app });
    }

    pub fn as(self: *ShellyWindow, comptime T: type) *T {
        return gobject.ext.as(T, self);
    }

    fn private(self: *ShellyWindow) *Private {
        return gobject.ext.impl_helpers.getPrivate(self, Private, Private.offset);
    }

    fn init(self: *ShellyWindow, _: *Class) callconv(.c) void {
        gtk.Widget.initTemplate(self.as(gtk.Widget));
        const p = self.private();
        p.nav_buttons = .empty;
        p.aur_slot_atoll = false;
        populateStack(self);
        setupMenu(self);
        applyConfig(self);
        applyDefaultPage(self);
        showWelcomeIfFirstStart(self);
        checkPolkitLockout(self);
        _ = gtk.Window.signals.close_request.connect(self.as(gtk.Window), *ShellyWindow, &on_close_request, self, .{});
    }

    pub fn applyConfig(self: *ShellyWindow) void {
        const svc = runtime.config orelse return;
        const cfg = svc.get() catch return;

        if (features.builtWithAur) {
            const use_atoll = atollAurEnabled();
            if (use_atoll != self.private().aur_slot_atoll) {
                swapAurSlot(self, use_atoll);
                syncAurUrl(use_atoll);
            }
        }

        setNavEnabled(self, "recommend", cfg.RecommendedEnabled);
        setNavEnabled(self, "aur", cfg.AurEnabled);
        setNavEnabled(self, "flatpak", cfg.FlatPackEnabled);
        setNavEnabled(self, "appimage", cfg.AppImageEnabled);
        setNavEnabled(self, "search", cfg.ShellySearchEnabled);
        gtk.Widget.setVisible(
            self.private().header_search_button.as(gtk.Widget),
            @intFromBool(features.builtWithShellySearch and cfg.ShellySearchEnabled),
        );
        self.setSoftwareMode(cfg.SoftwareMode);

        if (cfg.WindowLastWidth > 0 and cfg.WindowLastHeight > 0) {
            gtk.Window.setDefaultSize(
                self.as(gtk.Window),
                @intCast(cfg.WindowLastWidth),
                @intCast(cfg.WindowLastHeight),
            );
        }
    }

    pub fn setSoftwareMode(self: *ShellyWindow, enabled: bool) void {
        const p = self.private();
        if (!features.builtWithDevario) {
            p.explore_page.setSoftwareMode(enabled);
            p.installed_page.setSoftwareMode(enabled);
            if (features.builtWithRecommend) p.recommend_page.setSoftwareMode(enabled);
        }
    }

    fn on_close_request(_: *gtk.Window, self: *ShellyWindow) callconv(.c) c_int {
        const svc = runtime.config orelse return 0;
        const w = gtk.Widget.getWidth(self.as(gtk.Widget));
        const h = gtk.Widget.getHeight(self.as(gtk.Widget));
        if (w > 0 and h > 0) {
            const cfg = svc.get() catch return 0;
            var updated = cfg.*;
            updated.WindowLastWidth = w;
            updated.WindowLastHeight = h;
            svc.set(updated) catch return 0;
            svc.save() catch return 0;
        }
        return 0;
    }

    fn showWelcomeIfFirstStart(self: *ShellyWindow) void {
        const svc = runtime.config orelse return;
        const cfg = svc.get() catch return;
        if (!cfg.NewInstall) return;
        const wp = WelcomePage.new();
        self.showLockoutNamed(wp.as(gtk.Widget), translations._("Welcome to Shelly v3"));
    }

    fn checkPolkitLockout(self: *ShellyWindow) void {
        var dbus = DBus{};
        defer dbus.deinit();

        const status = dbus.checkPolkitStatus();
        if (status == .no_agent or status == .no_daemon) {
            const dialog = PolkitDialog.new(&on_polkit_close, self);
            self.showLockoutNamed(dialog.as(gtk.Widget), translations._("polkit is required"));
        }
    }

    fn on_polkit_close(ctx: ?*anyopaque) void {
        const self: *ShellyWindow = @ptrCast(@alignCast(ctx.?));
        self.hideLockout();
    }

    fn primaryPageName(name: [:0]const u8) ?[:0]const u8 {
        if (std.mem.eql(u8, name, "explore") or std.mem.eql(u8, name, "package") or std.mem.eql(u8, name, "recommend")) return "explore";
        if (std.mem.eql(u8, name, "installed")) return "installed";
        if (std.mem.eql(u8, name, "update") or std.mem.eql(u8, name, "updates")) return "update";
        return null;
    }

    fn canShowTopLevelPage(self: *ShellyWindow, name: [:0]const u8) bool {
        const p = self.private();
        if (primaryPageName(name)) |primary_name| {
            const child = adw.ViewStack.getChildByName(p.primary_stack, primary_name) orelse return false;
            const page = adw.ViewStack.getPage(p.primary_stack, child);
            return adw.ViewStackPage.getVisible(page) != 0;
        }

        const child = gtk.Stack.getChildByName(p.content_stack, name) orelse return false;
        const page = gtk.Stack.getPage(p.content_stack, child);
        return gtk.StackPage.getVisible(page) != 0;
    }

    fn showTopLevelPage(self: *ShellyWindow, name: [:0]const u8) bool {
        if (!self.canShowTopLevelPage(name)) return false;
        const p = self.private();
        if (primaryPageName(name)) |primary_name| {
            gtk.Stack.setVisibleChildName(p.content_stack, "primary");
            adw.ViewStack.setVisibleChildName(p.primary_stack, primary_name);
        } else {
            gtk.Stack.setVisibleChildName(p.content_stack, name);
        }
        return true;
    }

    fn setNavEnabled(self: *ShellyWindow, name: [:0]const u8, enabled: bool) void {
        const p = self.private();
        if (std.mem.eql(u8, name, "recommend")) {
            if (!features.builtWithDevario) p.explore_page.setRecommendationsEnabled(enabled);
            return;
        }

        for (p.nav_buttons.items) |nb| {
            if (!std.mem.eql(u8, nb.name, name)) continue;
            gtk.Widget.setVisible(nb.button.as(gtk.Widget), @intFromBool(enabled));
        }

        const child = gtk.Stack.getChildByName(p.content_stack, name) orelse return;
        const page = gtk.Stack.getPage(p.content_stack, child);
        gtk.StackPage.setVisible(page, @intFromBool(enabled));
        if (enabled) return;

        const visible = gtk.Stack.getVisibleChildName(p.content_stack);
        if (visible) |current| {
            if (std.mem.eql(u8, std.mem.span(current), name)) _ = self.showTopLevelPage("explore");
        }
    }

    pub fn requestNav(self: *ShellyWindow, mode: NavMode) void {
        _ = self;
        _ = mode;
    }

    pub fn changeNav(self: *ShellyWindow, mode: NavMode) void {
        _ = self;
        _ = mode;
    }

    fn setupMenu(self: *ShellyWindow) void {
        const p = self.private();
        if (features.builtWithAur) {
            addNavButton(self, p.aur_button, "aur");
        } else {
            gtk.Widget.setVisible(p.aur_button.as(gtk.Widget), 0);
        }
        if (features.builtWithFlatpak) {
            addNavButton(self, p.flatpak_button, "flatpak");
        } else {
            gtk.Widget.setVisible(p.flatpak_button.as(gtk.Widget), 0);
        }
        if (features.builtWithAppImage) {
            addNavButton(self, p.appimage_button, "appimage");
        } else {
            gtk.Widget.setVisible(p.appimage_button.as(gtk.Widget), 0);
        }
        if (features.builtWithShellySearch) {
            addNavButton(self, p.search_button, "search");
        } else {
            gtk.Widget.setVisible(p.search_button.as(gtk.Widget), 0);
        }
        gtk.Widget.setVisible(p.header_search_button.as(gtk.Widget), @intFromBool(features.builtWithShellySearch));
        _ = gtk.Button.signals.clicked.connect(p.utilities_button, *ShellyWindow, &onUtilitiesClicked, self, .{});
        _ = gtk.Button.signals.clicked.connect(p.settings_button, *ShellyWindow, &onSettingsClicked, self, .{});
        _ = gtk.Button.signals.clicked.connect(p.header_back_button, *ShellyWindow, &onHeaderBackClicked, self, .{});
        _ = gtk.Button.signals.clicked.connect(p.header_search_button, *ShellyWindow, &onSearchHeaderClicked, self, .{});
        _ = gtk.Button.signals.clicked.connect(p.settings_button, *ShellyWindow, &onSettingsClicked, self, .{});
        _ = gtk.Button.signals.clicked.connect(p.about_button, *ShellyWindow, &onAbout, self, .{});
    }

    pub fn showFullPackageDetail(
        self: *ShellyWindow,
        name: []const u8,
        is_installed: bool,
        icon_path: ?[:0]const u8,
    ) void {
        const p = self.private();
        p.full_detail_page.showPackage(name, is_installed, icon_path);
        p.full_detail_page.setInternalBackVisible(false);
        gtk.Widget.setVisible(p.header_back_button.as(gtk.Widget), 1);
        gtk.Widget.setVisible(p.full_detail_overlay.as(gtk.Widget), 1);
        gtk.Revealer.setRevealChild(p.full_detail_overlay, 1);
    }

    fn onFullDetailBack(context: ?*anyopaque) void {
        const self: *ShellyWindow = @ptrCast(@alignCast(context orelse return));
        hideFullDetail(self);
    }

    fn onHeaderBackClicked(_: *gtk.Button, self: *ShellyWindow) callconv(.c) void {
        const p = self.private();
        if (!p.full_detail_page.goBack()) hideFullDetail(self);
    }

    fn hideFullDetail(self: *ShellyWindow) void {
        const p = self.private();
        gtk.Widget.setVisible(p.header_back_button.as(gtk.Widget), 0);
        gtk.Revealer.setRevealChild(p.full_detail_overlay, 0);
        gtk.Widget.setVisible(p.full_detail_overlay.as(gtk.Widget), 0);
    }

    fn onSearchHeaderClicked(_: *gtk.Button, self: *ShellyWindow) callconv(.c) void {
        _ = self.showTopLevelPage("search");
    }

    pub fn presentSettings(self: *ShellyWindow) void {
        const p = self.private();
        adw.Dialog.present(p.settings_dialog, self.as(gtk.Widget));
    }

    fn onSettingsPresentOnce(user_data: ?*anyopaque) callconv(.c) void {
        const self: *ShellyWindow = @ptrCast(@alignCast(user_data orelse return));
        self.presentSettings();
    }

    fn onSettingsClicked(_: *gtk.Button, self: *ShellyWindow) callconv(.c) void {
        popdownMenu(self.private().settings_button);
        _ = glib.idleAddOnce(&onSettingsPresentOnce, self);
    }

    pub fn presentUtilities(self: *ShellyWindow) void {
        const p = self.private();
        adw.Dialog.present(p.utilities_dialog, self.as(gtk.Widget));
    }

    fn onUtilitiesPresentOnce(user_data: ?*anyopaque) callconv(.c) void {
        const self: *ShellyWindow = @ptrCast(@alignCast(user_data orelse return));
        self.presentUtilities();
    }

    fn onUtilitiesClicked(_: *gtk.Button, self: *ShellyWindow) callconv(.c) void {
        popdownMenu(self.private().utilities_button);
        _ = glib.idleAddOnce(&onUtilitiesPresentOnce, self);
    }

    fn addNavButton(self: *ShellyWindow, button: *gtk.Button, name: [:0]const u8) void {
        const nb = std.heap.c_allocator.create(NavButton) catch unreachable;
        nb.* = .{ .button = button, .name = name, .window = self };
        self.private().nav_buttons.append(std.heap.c_allocator, nb) catch unreachable;
        _ = gtk.Button.signals.clicked.connect(button, *NavButton, &onNavClick, nb, .{});
    }

    fn popdownMenu(button: *gtk.Button) void {
        if (gtk.Widget.getAncestor(button.as(gtk.Widget), gtk.Popover.getGObjectType())) |popover| {
            gtk.Popover.popdown(@ptrCast(@alignCast(popover)));
        }
    }

    fn onNavClick(button: *gtk.Button, nb: *NavButton) callconv(.c) void {
        _ = nb.window.showTopLevelPage(nb.name);
        popdownMenu(button);
    }

    fn onAbout(button: *gtk.Button, self: *ShellyWindow) callconv(.c) void {
        if (gtk.Window.getApplication(self.as(gtk.Window))) |app| {
            gio.ActionGroup.activateAction(app.as(gio.ActionGroup), "about", null);
        }
        popdownMenu(button);
    }

    pub fn applyDefaultPage(self: *ShellyWindow) void {
        const svc = runtime.config orelse {
            _ = self.showTopLevelPage("explore");
            return;
        };
        const cfg = svc.get() catch {
            _ = self.showTopLevelPage("explore");
            return;
        };

        switch (cfg.DefaultPageDropDown) {
            .packages, .recommend => {
                _ = self.showTopLevelPage("explore");
                if (!features.builtWithDevario) {
                    if (cfg.RecommendedEnabled) {
                        self.private().explore_page.showRecommended();
                    } else {
                        self.private().explore_page.showPackageBrowser();
                    }
                }
            },
            .update => _ = self.showTopLevelPage("update"),
            .aur => _ = self.showTopLevelPage("aur"),
            .flatpak => _ = self.showTopLevelPage("flatpak"),
            .app_image => _ = self.showTopLevelPage("appimage"),
            .shelly_search => _ = self.showTopLevelPage("search"),
        }
    }

    fn populateStack(self: *ShellyWindow) void {
        const p = self.private();
        const primary = p.primary_stack;
        const auxiliary = p.content_stack;

        const full_detail = PackageDetail.new();
        p.full_detail_page = full_detail;
        gtk.Widget.setHexpand(full_detail.as(gtk.Widget), 1);
        gtk.Widget.setVexpand(full_detail.as(gtk.Widget), 1);
        gtk.Widget.setHalign(full_detail.as(gtk.Widget), .fill);
        gtk.Widget.setValign(full_detail.as(gtk.Widget), .fill);
        full_detail.setBackHandler(self, &onFullDetailBack);
        full_detail.setInternalBackVisible(false);
        gtk.Box.append(p.full_detail_content, full_detail.as(gtk.Widget));

        const explore = PackagePage.new();
        p.explore_page = explore;
        if (!features.builtWithDevario and features.builtWithRecommend) {
            const recommended = RecommendPage.new();
            p.recommend_page = recommended;
            explore.setRecommendWidget(recommended.as(gtk.Widget));
        }
        _ = adw.ViewStack.addTitledWithIcon(primary, explore.as(gtk.Widget), "explore", translations._("Explore"), "globe-symbolic");

        const installed = if (features.builtWithDevario) PackagePage.new() else PackagePage.newInstalled();
        p.installed_page = installed;
        _ = adw.ViewStack.addTitledWithIcon(primary, installed.as(gtk.Widget), "installed", translations._("Installed"), "system-software-install-symbolic");

        const updates = UpdatePage.new();
        _ = adw.ViewStack.addTitledWithIcon(primary, updates.as(gtk.Widget), "update", translations._("Updates"), UpdatePage.icon_name);

        if (features.builtWithFlatpak) {
            const page = FlatpakPage.new();
            p.flatpak_page = page;
            const stack_page = gtk.Stack.addTitled(auxiliary, page.as(gtk.Widget), "flatpak", translations._("Flatpak"));
            gtk.StackPage.setIconName(stack_page, FlatpakPage.icon_name);
        }

        if (features.builtWithAppImage) {
            const page = AppImagePage.new();
            p.appimage_page = page;
            const stack_page = gtk.Stack.addTitled(auxiliary, page.as(gtk.Widget), "appimage", translations._("AppImage"));
            gtk.StackPage.setIconName(stack_page, AppImagePage.icon_name);
        }

        if (features.builtWithAur) {
            const stack_page = gtk.Stack.addTitled(auxiliary, aurSlotWidget(self), "aur", translations._(aur_title));
            gtk.StackPage.setIconName(stack_page, aur_icon_name);
        }

        if (features.builtWithShellySearch) {
            const page = ShellySearchPage.new();
            const stack_page = gtk.Stack.addTitled(auxiliary, page.as(gtk.Widget), "search", translations._(ShellySearchPage.title));
            gtk.StackPage.setIconName(stack_page, ShellySearchPage.icon_name);
        }

        const settings = SettingsPage.new();
        p.settings_page = settings;
        p.settings_dialog = adw.Dialog.new();
        const settings_switcher = gtk.StackSwitcher.new();
        gtk.StackSwitcher.setStack(settings_switcher, settings.settingsStack());
        const settings_toolbar = adw.ToolbarView.new();
        const settings_header = adw.HeaderBar.new();
        adw.HeaderBar.setTitleWidget(settings_header, settings_switcher.as(gtk.Widget));
        adw.ToolbarView.addTopBar(settings_toolbar, settings_header.as(gtk.Widget));
        adw.ToolbarView.setContent(settings_toolbar, settings.as(gtk.Widget));
        adw.Dialog.setChild(p.settings_dialog, settings_toolbar.as(gtk.Widget));
        adw.Dialog.setContentWidth(p.settings_dialog, 760);
        adw.Dialog.setContentHeight(p.settings_dialog, 640);

        const utilities = UtilitiesPage.new();
        p.utilities_page = utilities;
        p.utilities_dialog = adw.Dialog.new();
        adw.Dialog.setTitle(p.utilities_dialog, translations._("Utilities"));
        const utilities_toolbar = adw.ToolbarView.new();
        const utilities_header = adw.HeaderBar.new();
        adw.ToolbarView.addTopBar(utilities_toolbar, utilities_header.as(gtk.Widget));
        adw.ToolbarView.setContent(utilities_toolbar, utilities.as(gtk.Widget));
        adw.Dialog.setChild(p.utilities_dialog, utilities_toolbar.as(gtk.Widget));
        adw.Dialog.setContentWidth(p.utilities_dialog, 720);
        adw.Dialog.setContentHeight(p.utilities_dialog, 560);

        gtk.Stack.setVisibleChildName(auxiliary, "primary");
        adw.ViewStack.setVisibleChildName(primary, "explore");
    }

    fn aurSlotWidget(self: *ShellyWindow) *gtk.Widget {
        const use_atoll = atollAurEnabled();
        self.private().aur_slot_atoll = use_atoll;
        return newAurSlotWidget(use_atoll);
    }

    fn newAurSlotWidget(use_atoll: bool) *gtk.Widget {
        if (features.builtWithBuiltinAur) {
            if (features.builtWithAtoll and use_atoll) return AtollAurPage.new().as(gtk.Widget);
            return AurPage.new().as(gtk.Widget);
        }
        return AtollAurPage.new().as(gtk.Widget);
    }

    fn atollAurEnabled() bool {
        if (features.builtWithAtollOnly) return true;
        if (!features.builtWithAtoll) return false;
        const svc = runtime.config orelse return false;
        const cfg = svc.get() catch return false;
        return cfg.AtollAurEnabled;
    }

    fn syncAurUrl(use_atoll: bool) void {
        const url = if (use_atoll) AtollApiService.base_url else default_aur_base;

        var arena = std.heap.ArenaAllocator.init(std.heap.c_allocator);
        defer arena.deinit();

        const cli = ShellyCli{ .allocator = arena.allocator(), .io = runtime.io };
        cli.set_aur_url(url) catch |err| {
            std.log.warn("Could not set the AUR base URL to {0s}. {1s}\n\nTechnical details: {2s}", .{ url, diagnostics.cause(err), @errorName(err) });
        };
    }

    fn swapAurSlot(self: *ShellyWindow, use_atoll: bool) void {
        const p = self.private();
        const stack = p.content_stack;

        const was_visible = if (gtk.Stack.getVisibleChildName(stack)) |name|
            std.mem.eql(u8, std.mem.span(name), "aur")
        else
            false;

        if (gtk.Stack.getChildByName(stack, "aur")) |child| gtk.Stack.remove(stack, child);

        const page = gtk.Stack.addTitled(stack, newAurSlotWidget(use_atoll), "aur", translations._(aur_title));
        gtk.StackPage.setIconName(page, aur_icon_name);
        p.aur_slot_atoll = use_atoll;

        if (was_visible) gtk.Stack.setVisibleChildName(stack, "aur");
    }

    pub fn showLockout(self: *ShellyWindow, content: *gtk.Widget) void {
        self.mountLockout(content, "");
    }

    pub fn showLockoutNamed(self: *ShellyWindow, content: *gtk.Widget, name: []const u8) void {
        self.mountLockout(content, name);
    }

    fn mountLockout(self: *ShellyWindow, content: *gtk.Widget, name: []const u8) void {
        const p = self.private();
        while (gtk.Widget.getFirstChild(p.lockout_content.as(gtk.Widget))) |child| {
            gtk.Box.remove(p.lockout_content, child);
        }
        gtk.Box.append(p.lockout_content, content);
        a11y.setName(p.lockout_overlay.as(gtk.Widget), name);

        gtk.Widget.setSensitive(p.content_stack.as(gtk.Widget), 0);
        gtk.Widget.setSensitive(p.header_bar.as(gtk.Widget), 0);
        gtk.Widget.setVisible(p.lockout_overlay.as(gtk.Widget), 1);

        if (gobject.ext.cast(ConfirmDialog, content)) |dialog| {
            dialog.focusConfirm();
        } else {
            _ = a11y.focusFirst(content);
        }
    }

    pub fn hideLockout(self: *ShellyWindow) void {
        const p = self.private();
        gtk.Widget.setVisible(p.lockout_overlay.as(gtk.Widget), 0);
        gtk.Widget.setSensitive(p.content_stack.as(gtk.Widget), 1);
        gtk.Widget.setSensitive(p.header_bar.as(gtk.Widget), 1);

        while (gtk.Widget.getFirstChild(p.lockout_content.as(gtk.Widget))) |child| {
            gtk.Box.remove(p.lockout_content, child);
        }

        const visible = gtk.Stack.getVisibleChildName(p.content_stack);
        if (visible) |name| {
            if (std.mem.eql(u8, std.mem.span(name), "primary")) {
                _ = a11y.focusFirst(p.view_switcher.as(gtk.Widget));
            } else if (gtk.Stack.getVisibleChild(p.content_stack)) |child| {
                _ = a11y.focusFirst(child);
            }
        }
    }

    pub fn startTransaction(self: *ShellyWindow, request: TransactionRequest) void {
        const page = TransactionPage.new();
        self.showLockout(page.as(gtk.Widget));
        page.run(request);
    }

    pub fn openFlatpakApp(self: *ShellyWindow, app_id: [:0]const u8) bool {
        if (features.builtWithFlatpak) {
            const p = self.private();
            if (gtk.Widget.getVisible(p.lockout_overlay.as(gtk.Widget)) != 0) return false;
            if (!self.canShowTopLevelPage("flatpak")) return false;
            p.flatpak_page.openApp(app_id);
            return self.showTopLevelPage("flatpak");
        }
        return false;
    }

    pub fn openFlatpakLocalFile(self: *ShellyWindow, path: [:0]const u8) bool {
        if (features.builtWithFlatpak) {
            const p = self.private();
            if (gtk.Widget.getVisible(p.lockout_overlay.as(gtk.Widget)) != 0) return false;
            if (!self.canShowTopLevelPage("flatpak")) return false;
            p.flatpak_page.openLocalFile(path);
            return self.showTopLevelPage("flatpak");
        }
        return false;
    }

    pub fn openAppImageLocalFile(self: *ShellyWindow, path: [:0]const u8) bool {
        if (features.builtWithAppImage) {
            const p = self.private();
            if (gtk.Widget.getVisible(p.lockout_overlay.as(gtk.Widget)) != 0) return false;
            if (!self.canShowTopLevelPage("appimage")) return false;
            p.appimage_page.openLocalFile(path);
            return self.showTopLevelPage("appimage");
        }
        return false;
    }

    pub fn navigateTo(self: *ShellyWindow, target: deep_link.PageTarget) bool {
        return switch (target) {
            .updates => self.showTopLevelPage("update"),
            .flatpak_install => blk: {
                if (features.builtWithFlatpak) {
                    if (!self.canShowTopLevelPage("flatpak")) break :blk false;
                    self.private().flatpak_page.navigateTo(.install);
                    break :blk self.showTopLevelPage("flatpak");
                }
                break :blk false;
            },
            .flatpak_remove => blk: {
                if (features.builtWithFlatpak) {
                    if (!self.canShowTopLevelPage("flatpak")) break :blk false;
                    self.private().flatpak_page.navigateTo(.remove);
                    break :blk self.showTopLevelPage("flatpak");
                }
                break :blk false;
            },
        };
    }

    const template_children = .{
        .{ "header_bar", @offsetOf(Private, "header_bar") },
        .{ "view_switcher", @offsetOf(Private, "view_switcher") },
        .{ "content_stack", @offsetOf(Private, "content_stack") },
        .{ "primary_stack", @offsetOf(Private, "primary_stack") },
        .{ "lockout_overlay", @offsetOf(Private, "lockout_overlay") },
        .{ "lockout_content", @offsetOf(Private, "lockout_content") },
        .{ "full_detail_overlay", @offsetOf(Private, "full_detail_overlay") },
        .{ "full_detail_content", @offsetOf(Private, "full_detail_content") },
        .{ "header_back_button", @offsetOf(Private, "header_back_button") },
        .{ "header_search_button", @offsetOf(Private, "header_search_button") },
        .{ "aur_menu_button", @offsetOf(Private, "aur_button") },
        .{ "flatpak_menu_button", @offsetOf(Private, "flatpak_button") },
        .{ "appimage_menu_button", @offsetOf(Private, "appimage_button") },
        .{ "search_menu_button", @offsetOf(Private, "search_button") },
        .{ "utilities_menu_button", @offsetOf(Private, "utilities_button") },
        .{ "settings_menu_button", @offsetOf(Private, "settings_button") },
        .{ "about_menu_button", @offsetOf(Private, "about_button") },
    };

    pub const Class = extern struct {
        parent_class: Parent.Class,
        var parent: *Parent.Class = undefined;
        pub const Instance = ShellyWindow;

        fn init(class: *Class) callconv(.c) void {
            const widget_class = gobject.ext.as(gtk.Widget.Class, class);
            gtk.Widget.Class.setTemplateFromResource(widget_class, "/com/shellyorg/shelly/ui/main_window.ui");
            inline for (template_children) |child| {
                SupportPage.bindChild(class, Private.offset, child[0], child[1]);
            }
            gobject.Object.virtual_methods.finalize.implement(class, &finalize);
        }

        pub fn as(class: *Class, comptime T: type) *T {
            return gobject.ext.as(T, class);
        }
    };

    fn finalize(self: *ShellyWindow) callconv(.c) void {
        const p = self.private();
        for (p.nav_buttons.items) |button| std.heap.c_allocator.destroy(button);
        p.nav_buttons.deinit(std.heap.c_allocator);
        gobject.Object.virtual_methods.finalize.call(Class.parent, self.as(Parent));
    }
};
