using System.Text.RegularExpressions;

namespace Shelly.Gtk.Services;

public class LockoutService : ILockoutService
{
    private readonly Lock _lock = new();

    public event EventHandler<ILockoutService.LockoutStatusEventArgs>? StatusChanged;
    public event EventHandler<string>? LogLineReceived;

    private bool IsLocked { get; set; }
    private double Progress { get; set; }
    private bool IsIndeterminate { get; set; } = true;
    private string? Description { get; set; }

    public void Show(string description, double progress = 0, bool isIndeterminate = true)
    {
        lock (_lock)
        {
            IsLocked = true;
            Description = description;
            Progress = progress;
            IsIndeterminate = isIndeterminate;
        }

        NotifyChanged();
    }

    public void Hide()
    {
        lock (_lock)
        {
            IsLocked = false;
        }

        NotifyChanged();
    }

    public void ParseLog(string? logLine)
    {
        LogLineReceived?.Invoke(this, logLine ?? "");
    }

    private void NotifyChanged()
    {
        bool locked;
        double prog;
        bool indet;
        string? desc;

        lock (_lock)
        {
            locked = IsLocked;
            prog = Progress;
            indet = IsIndeterminate;
            desc = Description;
        }

        StatusChanged?.Invoke(this, new ILockoutService.LockoutStatusEventArgs
        {
            IsLocked = locked,
            Description = desc,
            Progress = prog,
            IsIndeterminate = indet
        });
    }
}