
namespace PackageManager.Flatpak.Models;

public record FlatpakInstanceDto
{
    public string Name { get; set; } = string.Empty;
    public string AppId { get; set; } = string.Empty;
    public int Pid { get; set; }
}