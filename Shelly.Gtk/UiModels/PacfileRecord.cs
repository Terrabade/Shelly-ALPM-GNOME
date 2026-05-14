using MemoryPack;

namespace Shelly.Gtk.UiModels;

[MemoryPackable]
public partial record PacfileRecord(string Name,string Text);