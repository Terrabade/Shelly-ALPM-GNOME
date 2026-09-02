const std = @import("std");
const builtin = @import("builtin");

const Io = std.Io;
const HostName = Io.net.HostName;
const posix = std.posix;
const can_use_libc = builtin.os.tag == .linux and builtin.link_libc;
const max_resolv_conf_size = 64 * 1024;

/// Zig 0.16's native Linux resolver stores a complete `search` or `domain`
/// directive from `/etc/resolv.conf` in a `HostName.max_len` buffer. Such a
/// directive can legally be longer than one DNS name, so use libc only for
/// that unsafe input. Ordinary configurations retain Zig's cancellable native
/// resolver.
pub fn lookup(
    host_name: HostName,
    io: Io,
    resolved: *Io.Queue(HostName.LookupResult),
    options: HostName.LookupOptions,
) HostName.LookupError!void {
    if (comptime can_use_libc) {
        // Keep Zig's cancellable resolver on ordinary Linux configurations.
        // libc is a targeted fallback for the oversized search-list input that
        // Zig 0.16 cannot currently parse safely.
        if (needsLibcResolver(io))
            return lookupLibc(host_name, io, resolved, options);
    }
    return HostName.lookup(host_name, io, resolved, options);
}

fn needsLibcResolver(io: Io) bool {
    const payload = std.Io.Dir.cwd().readFileAlloc(
        io,
        "/etc/resolv.conf",
        std.heap.page_allocator,
        .limited(max_resolv_conf_size),
    ) catch |err| return err == error.StreamTooLong;
    defer std.heap.page_allocator.free(payload);
    return hasOversizedSearchOrDomainDirective(payload);
}

fn hasOversizedSearchOrDomainDirective(payload: []const u8) bool {
    var lines = std.mem.splitScalar(u8, payload, '\n');
    while (lines.next()) |raw_line| {
        const line = std.mem.sliceTo(raw_line, '#');
        var tokens = std.mem.tokenizeAny(u8, line, " \t\r");
        const directive = tokens.next() orelse continue;
        if (!std.mem.eql(u8, directive, "search") and
            !std.mem.eql(u8, directive, "domain")) continue;
        if (tokens.rest().len > HostName.max_len) return true;
    }
    return false;
}

fn lookupLibc(
    host_name: HostName,
    io: Io,
    resolved: *Io.Queue(HostName.LookupResult),
    options: HostName.LookupOptions,
) HostName.LookupError!void {
    defer resolved.close(io);

    var name_buffer: [HostName.max_len:0]u8 = undefined;
    @memcpy(name_buffer[0..host_name.bytes.len], host_name.bytes);
    name_buffer[host_name.bytes.len] = 0;
    const name = name_buffer[0..host_name.bytes.len :0];

    const hints: posix.addrinfo = .{
        .flags = .{ .CANONNAME = options.canonical_name_buffer != null },
        .family = if (options.family) |family| switch (family) {
            .ip4 => posix.AF.INET,
            .ip6 => posix.AF.INET6,
        } else posix.AF.UNSPEC,
        .socktype = posix.SOCK.STREAM,
        .protocol = posix.IPPROTO.TCP,
        .canonname = null,
        .addr = null,
        .addrlen = 0,
        .next = null,
    };

    var result: ?*posix.addrinfo = null;
    while (true) {
        switch (std.c.getaddrinfo(name.ptr, null, &hints, &result)) {
            @as(std.c.EAI, @enumFromInt(0)) => break,
            .SYSTEM => switch (posix.errno(-1)) {
                .INTR => continue,
                else => return error.Unexpected,
            },
            .ADDRFAMILY, .FAMILY => return error.AddressFamilyUnsupported,
            .AGAIN, .FAIL => return error.NameServerFailure,
            .MEMORY => return error.SystemResources,
            .NODATA, .NONAME => return error.UnknownHostName,
            .CANCELED => return error.Canceled,
            else => return error.Unexpected,
        }
    }
    defer if (result) |head| std.c.freeaddrinfo(head);

    var address_count: usize = 0;
    var canonical_name: ?[*:0]const u8 = null;
    var current = result;
    while (current) |info| : (current = info.next) {
        const address = info.addr orelse continue;
        switch (address.family) {
            posix.AF.INET, posix.AF.INET6 => {},
            else => continue,
        }

        var ip_address = Io.Threaded.addressFromPosix(
            @alignCast(@fieldParentPtr("any", address)),
        );
        ip_address.setPort(options.port);
        try putResult(resolved, io, .{ .address = ip_address });
        address_count += 1;

        if (canonical_name == null) canonical_name = info.canonname;
    }

    if (options.canonical_name_buffer) |buffer| {
        if (canonical_name) |canonical| {
            const bytes = std.mem.sliceTo(canonical, 0);
            if (bytes.len <= buffer.len) {
                const destination = buffer[0..bytes.len];
                @memcpy(destination, bytes);
                try putResult(resolved, io, .{
                    .canonical_name = .{ .bytes = destination },
                });
            }
        }
    }

    if (address_count == 0) return error.NoAddressReturned;
}

fn putResult(
    resolved: *Io.Queue(HostName.LookupResult),
    io: Io,
    result: HostName.LookupResult,
) HostName.LookupError!void {
    resolved.putOne(io, result) catch |err| switch (err) {
        error.Canceled => return error.Canceled,
        error.Closed => unreachable,
    };
}

test "Linux resolver reserves libc for oversized search and domain directives" {
    if (builtin.os.tag != .linux) return error.SkipZigTest;
    try std.testing.expect(can_use_libc);
    try std.testing.expect(!hasOversizedSearchOrDomainDirective(
        "search home\nnameserver 127.0.0.53\n",
    ));

    var payload: ["search ".len + HostName.max_len + 2]u8 = undefined;
    @memcpy(payload[0.."search ".len], "search ");
    @memset(payload["search ".len .. payload.len - 1], 'a');
    payload[payload.len - 1] = '\n';
    try std.testing.expect(hasOversizedSearchOrDomainDirective(&payload));

    payload[0.."domain ".len].* = "domain ".*;
    try std.testing.expect(hasOversizedSearchOrDomainDirective(&payload));
}
