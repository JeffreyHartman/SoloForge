using System.Diagnostics;
using System.Runtime.InteropServices;
using Serilog;

namespace SoloForge.Console.Services;

/// <summary>
/// Service for copying text to the system clipboard.
/// Uses singleton pattern for global access.
/// </summary>
public sealed class ClipboardService
{
    private static readonly Lazy<ClipboardService> _instance = new(() => new ClipboardService());
    public static ClipboardService Instance => _instance.Value;

    private readonly ILogger _log = AppLogger.ForContext<ClipboardService>();
    private readonly string? _linuxClipboardCommand;

    private ClipboardService()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            _linuxClipboardCommand = DetectLinuxClipboardCommand();
            if (_linuxClipboardCommand != null)
            {
                _log.Information("Linux clipboard command detected: {Command}", _linuxClipboardCommand);
            }
            else
            {
                _log.Warning("No clipboard command found on Linux. Install xsel, xclip, or wl-copy for clipboard support");
            }
        }
        _log.Debug("ClipboardService initialized");
    }

    /// <summary>
    /// Copies text to the system clipboard.
    /// </summary>
    /// <param name="text">The text to copy.</param>
    /// <returns>True if the copy succeeded, false otherwise.</returns>
    public bool CopyToClipboard(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            _log.Warning("CopyToClipboard called with null or empty text");
            return false;
        }

        _log.Debug("Attempting to copy {Length} characters to clipboard", text.Length);

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return CopyToLinuxClipboard(text);
            }

            // Use TextCopy for Windows and macOS
            new TextCopy.Clipboard().SetText(text);
            _log.Information("Successfully copied {Length} characters to clipboard", text.Length);
            return true;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to copy to clipboard. Text length: {Length}", text.Length);
            return false;
        }
    }

    /// <summary>
    /// Copies text to the clipboard asynchronously.
    /// </summary>
    public async Task<bool> CopyToClipboardAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            _log.Warning("CopyToClipboardAsync called with null or empty text");
            return false;
        }

        _log.Debug("Attempting async copy of {Length} characters to clipboard", text.Length);

        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return await Task.Run(() => CopyToLinuxClipboard(text));
            }

            await new TextCopy.Clipboard().SetTextAsync(text);
            _log.Information("Successfully copied {Length} characters to clipboard (async)", text.Length);
            return true;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to copy to clipboard (async). Text length: {Length}", text.Length);
            return false;
        }
    }

    private bool CopyToLinuxClipboard(string text)
    {
        if (_linuxClipboardCommand == null)
        {
            _log.Error("No clipboard command available on Linux. Install xsel, xclip, or wl-copy");
            return false;
        }

        try
        {
            var (command, args) = GetClipboardCommandArgs(_linuxClipboardCommand);

            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = args,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.StandardInput.Write(text);
            process.StandardInput.Close();
            process.WaitForExit(5000);

            if (process.ExitCode == 0)
            {
                _log.Information("Successfully copied {Length} characters to clipboard using {Command}", text.Length, _linuxClipboardCommand);
                return true;
            }

            var error = process.StandardError.ReadToEnd();
            _log.Error("Clipboard command failed with exit code {ExitCode}: {Error}", process.ExitCode, error);
            return false;
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Failed to execute clipboard command: {Command}", _linuxClipboardCommand);
            return false;
        }
    }

    private static string? DetectLinuxClipboardCommand()
    {
        // Check for Wayland first (wl-copy)
        if (Environment.GetEnvironmentVariable("WAYLAND_DISPLAY") != null)
        {
            if (CommandExists("wl-copy"))
                return "wl-copy";
        }

        // Check for X11 clipboard tools
        if (CommandExists("xclip"))
            return "xclip";

        if (CommandExists("xsel"))
            return "xsel";

        // Fallback to wl-copy even on X11 (some systems have both)
        if (CommandExists("wl-copy"))
            return "wl-copy";

        return null;
    }

    private static (string command, string args) GetClipboardCommandArgs(string clipboardCommand)
    {
        return clipboardCommand switch
        {
            "xclip" => ("xclip", "-selection clipboard"),
            "xsel" => ("xsel", "--clipboard --input"),
            "wl-copy" => ("wl-copy", ""),
            _ => (clipboardCommand, "")
        };
    }

    private static bool CommandExists(string command)
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(1000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
