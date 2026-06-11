using System.Diagnostics;

namespace Shelly.Gtk.Services.TrayServices;

public static class TrayStartService
{
    private const string AppName = "shelly-notifications";

    public static void Start()
    {
        try
        {
            const string appPath = "/usr/bin/shelly-notifications";
            // Installed manually using local-install.sh
            const string optPath = "/opt/shelly/Shelly-Notifications";

            var path = string.Empty;
            if (File.Exists(appPath)) path = appPath;
            if (File.Exists(optPath)) path = optPath;

            if (string.IsNullOrEmpty(path))
            {
                Console.WriteLine($"Tray service executable not found at: {optPath}");
                Console.WriteLine($"Tray service executable not found at: {appPath}");
                return;
            }

            try
            {
                var processes = Process.GetProcessesByName(AppName);
                Array.ForEach(processes, p => p.Dispose());
                if (processes.Length > 0)
                {
                    Console.WriteLine("Tray service is already running (process detected).");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to check running processes: {ex.Message}");
            }

            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            process.Start();
            Console.WriteLine("Tray service started successfully as detached process.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start tray service: {ex.Message}");
        }
    }

    public static void End()
    {
        Console.WriteLine("Closing shelly notifications.");
        try
        {
            var processes = Process.GetProcessesByName(AppName);
            foreach (var process in processes)
                using (process)
                {
                    try
                    {
                        process.Kill();
                        process.WaitForExit(TimeSpan.FromSeconds(2));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to kill {AppName} (PID: {process.Id}): {ex.Message}");
                    }
                }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while trying to kill app: {ex.Message}");
        }
    }
}