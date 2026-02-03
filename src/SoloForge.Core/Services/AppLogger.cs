using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace SoloForge.Console.Services;

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
        // Store logs in a 'logs' directory relative to the app
        var baseDir = AppContext.BaseDirectory;

        // For development, try to find project root
        var projectRoot = FindProjectRoot();
        if (projectRoot != null)
        {
            var devLogDir = Path.Combine(projectRoot, "logs");
            Directory.CreateDirectory(devLogDir);
            return Path.Combine(devLogDir, "soloforge-.log");
        }

        // Production: use app directory
        var logDir = Path.Combine(baseDir, "logs");
        Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, "soloforge-.log");
    }

    private static string? FindProjectRoot()
    {
        var currentDir = new DirectoryInfo(AppContext.BaseDirectory);

        while (currentDir != null)
        {
            // Look for the solution or project file
            if (File.Exists(Path.Combine(currentDir.FullName, "SoloForge.Console.csproj")) ||
                File.Exists(Path.Combine(currentDir.FullName, "src", "SoloForge.Console", "SoloForge.Console.csproj")))
            {
                // Return the repo root (parent of src)
                var srcPath = Path.Combine(currentDir.FullName, "src");
                if (Directory.Exists(srcPath))
                    return currentDir.FullName;
                return currentDir.Parent?.FullName;
            }

            currentDir = currentDir.Parent;
        }

        return null;
    }

    private static string? FindSettingsFile()
    {
        var baseDir = AppContext.BaseDirectory;
        var currentDir = new DirectoryInfo(baseDir);

        while (currentDir != null)
        {
            var settingsPath = Path.Combine(currentDir.FullName, "appsettings.json");
            if (File.Exists(settingsPath))
                return settingsPath;

            var srcPath = Path.Combine(currentDir.FullName, "src", "SoloForge.Console", "appsettings.json");
            if (File.Exists(srcPath))
                return srcPath;

            currentDir = currentDir.Parent;
        }

        return null;
    }
}
