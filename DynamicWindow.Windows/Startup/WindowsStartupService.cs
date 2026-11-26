using Microsoft.Win32;

namespace DynamicWindow.Windows.Startup;

public sealed class WindowsStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "DynamicWindow";

    public void SetEnabled(bool enabled)
    {
        try
        {
            using var runKey = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
            if (runKey is null) return;

            if (!enabled)
            {
                runKey.DeleteValue(ValueName, throwOnMissingValue: false);
                return;
            }

            var executable = Environment.ProcessPath;
            if (string.IsNullOrWhiteSpace(executable)) return;

            var command = $"\"{executable}\"";
            if (command.Length <= 260)
            {
                runKey.SetValue(ValueName, command, RegistryValueKind.String);
            }
        }
        catch
        {
            // Startup registration is best-effort; the app remains usable if Windows blocks it.
        }
    }
}
