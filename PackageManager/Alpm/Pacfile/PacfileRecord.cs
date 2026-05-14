using MemoryPack;

namespace PackageManager.Alpm.Pacfile;

[MemoryPackable]
public partial record PacfileRecord(string Name,string Text);