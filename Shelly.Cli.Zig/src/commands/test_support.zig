const std = @import("std");
const runtime = @import("../runtime/context.zig");

pub const TestContext = struct {
    arena: std.heap.ArenaAllocator = undefined,
    stdout: std.Io.Writer.Allocating = undefined,
    stderr: std.Io.Writer.Allocating = undefined,
    context: runtime.RuntimeContext = undefined,

    pub fn init(self: *TestContext) void {
        self.arena = std.heap.ArenaAllocator.init(std.testing.allocator);
        self.stdout = std.Io.Writer.Allocating.init(std.testing.allocator);
        self.stderr = std.Io.Writer.Allocating.init(std.testing.allocator);
        self.context = .{
            .allocator = self.arena.allocator(),
            .io = std.testing.io,
            .stdout = &self.stdout.writer,
            .stderr = &self.stderr.writer,
        };
    }

    pub fn deinit(self: *TestContext) void {
        self.stderr.deinit();
        self.stdout.deinit();
        self.arena.deinit();
    }
};
