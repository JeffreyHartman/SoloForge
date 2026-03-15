using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace SoloForge.Core.Services;

/// <summary>
/// Application logger service using Serilog.
/// Provides structured logging with file rotation and configurable log levels.
/// </summary>
public static class AppLogger
{
    private static bool _initialized;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the Serilog logger instance.
    /// </summary>
    public static ILogger Logger => Log.Logger;

    /// <summary>
    /// Initializes the logging system. Should be called once at application startup.
    /// </summary>
    public static void Initialize()
    {
        lock (_lock)
        {
            if (_initialized) return;

            var configuration = LoadConfiguration();
            var logPath = GetLogPath();

            var loggerConfig = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "SoloForge");

            // If no Serilog config in appsettings, use sensible defaults
            if (configuration.GetSection("Serilog").Exists() == false)
            {
                loggerConfig
                    .MinimumLevel.Information()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .WriteTo.File(
                        path: logPath,
                        rollingInterval: RollingInterval.Day,
                        fileSizeLimitBytes: 10 * 1024 * 1024, // 10 MB
                        retainedFileCountLimit: 7,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
                    );
            }

            Log.Logger = loggerConfig.CreateLogger();
            _initialized = true;

            Log.Information("=== SoloForge Application Started ===");
            Log.Information("Log file location: {LogPath}", logPath);
        }
    }

    /// <summary>
    /// Creates a logger for a specific type/class.
    /// </summary>
    public static ILogger ForContext<T>() => Log.ForContext<T>();

    /// <summary>
    /// Creates a logger for a specific type/class.
    /// </summary>
    public static ILogger ForContext(Type type) => Log.ForContext(type);

    /// <summary>
    /// Creates a logger with a custom source context.
    /// </summary>
    public static ILogger ForContext(string sourceContext) => Log.ForContext("SourceContext", sourceContext);

    /// <summary>
    /// Flushes and closes the logger. Should be called at application shutdown.
    /// </summary>
    public static void Shutdown()
    {
        Log.Information("=== SoloForge Application Shutdown ===");
        Log.CloseAndFlush();
    }

    private static IConfiguration LoadConfiguration()
    {
        var settingsPath = FindSettingsFile();

        var builder = new ConfigurationBuilder();

        if (!string.IsNullOrEmpty(settingsPath) && File.Exists(settingsPath))
        {
            builder.AddJsonFile(settingsPath, optional: true, reloadOnChange: false);
        }

        return builder.Build();
    }

    private static string GetLogPath()
    {
        var root = FindProjectRoot();
        var logDir = root != null
            ? Path.Combine(root, "logs")
            : Path.Combine(AppContext.BaseDirectory, "logs");

        Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, "soloforge-.log");
    }

    /// <summary>
    /// Returns the repo root (directory containing SoloForge.slnx), or null in production.
    /// </summary>
    public static string? FindProjectRoot()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDir != null)
        {
            if (File.Exists(Path.Combine(currentDir.FullName, "SoloForge.slnx")))
                return currentDir.FullName;

            currentDir = currentDir.Parent;
        }

        return null;
    }

    private static string? FindSettingsFile()
    {
        var root = FindProjectRoot();
        if (root != null)
        {
            var rootSettings = Path.Combine(root, "appsettings.json");
            if (File.Exists(rootSettings))
                return rootSettings;
        }

        var local = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        return File.Exists(local) ? local : null;
    }
}
