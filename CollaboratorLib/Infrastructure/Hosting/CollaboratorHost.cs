using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using StoryCollaborator.Services;
using Uno.Extensions;
using Uno.Extensions.Hosting;
using Uno.Extensions.Logging;

namespace StoryCollaborator.Infrastructure.Hosting;

/// <summary>
/// Provides isolated hosting infrastructure for the Collaborator plugin using Uno Extensions.
/// Maintains its own IHost instance separate from the StoryCAD host application.
/// </summary>
public static class CollaboratorHost
{
    private static IHost? _host;
    private static Task? _startTask;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the service provider from the host. Throws if host not created.
    /// </summary>
    public static IServiceProvider Services =>
        _host?.Services ?? throw new InvalidOperationException("CollaboratorHost not initialized. Call GetOrCreate() first.");

    /// <summary>
    /// Gets a required service from the host's service provider.
    /// Convenience method for service resolution.
    /// </summary>
    public static T GetRequiredService<T>() where T : notnull =>
        Services.GetRequiredService<T>();

    /// <summary>
    /// Gets or creates the singleton IHost instance for the Collaborator plugin.
    /// </summary>
    /// <returns>The IHost instance with configured services</returns>
    public static IHost GetOrCreate()
    {
        if (_host != null) return _host;

        lock (_lock)
        {
            if (_host != null) return _host;

            _host = BuildHost();

            // Start the host asynchronously to avoid blocking UI thread.
            _startTask = _host.StartAsync();

            // Log host creation via factory since CollaboratorHost is static
            try
            {
                var loggerFactory = _host.Services.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("CollaboratorHost");
                logger?.LogInformation("Collaborator host created");
            }
            catch
            {
                // Ignore - logging failure shouldn't prevent host creation
            }

            return _host;
        }
    }

    /// <summary>
    /// Ensures the host has fully started.
    /// Safe to call from UI thread - uses ConfigureAwait(false).
    /// </summary>
    public static async Task EnsureStartedAsync()
    {
        if (_startTask != null)
        {
            await _startTask.ConfigureAwait(false);

            try
            {
                var loggerFactory = _host?.Services.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("CollaboratorHost");
                logger?.LogInformation("Collaborator host started, navigation routes available");
            }
            catch { /* Ignore logging failures */ }
        }
    }

    /// <summary>
    /// Shuts down and disposes the host instance.
    /// </summary>
    public static async Task ShutdownAsync()
    {
        if (_host != null)
        {
            try
            {
                var loggerFactory = _host.Services.GetService<ILoggerFactory>();
                var logger = loggerFactory?.CreateLogger("CollaboratorHost");
                logger?.LogInformation("Collaborator host shutting down");
            }
            catch { /* Ignore logging failures */ }

            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
            _host = null;
            _startTask = null;
        }
    }

    private static IHost BuildHost()
    {
        return UnoHost.CreateDefaultBuilder()
            .UseLogging(ConfigureLogging)
            .ConfigureServices(ConfigureServices)
            .Build();
    }

    private static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
    {
        // Application services (SRP-compliant)
        services.AddSingleton<SessionService>();
        services.AddSingleton<WorkflowService>();
    }

    private static void ConfigureLogging(HostBuilderContext context, ILoggingBuilder builder)
    {
        builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);

        // Add console/debug providers
        builder.AddConsole();
        builder.AddDebug();

        // Configure NLog for file logging
        var nlogConfig = CreateNLogConfiguration();
        LogManager.Configuration = nlogConfig;
        builder.AddNLog(nlogConfig);

        // Filter for sensitive data (Semantic Kernel, etc.)
        builder.AddFilter((category, level) =>
        {
            if (category?.Contains("Semantic.Kernel") == true && level < Microsoft.Extensions.Logging.LogLevel.Warning)
                return false;
            return true;
        });
    }

    /// <summary>
    /// Creates NLog configuration with file target for Collaborator logs.
    /// Logs are stored in user's AppData/Local/Collaborator/logs (separate from StoryCAD).
    /// </summary>
    private static LoggingConfiguration CreateNLogConfiguration()
    {
        var config = new LoggingConfiguration();

        // File target - use Collaborator-specific path (NOT under StoryCAD)
        var logFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Collaborator",  // Separate from StoryCAD
            "logs");

        var fileTarget = new FileTarget("collaborator-file")
        {
            FileName = Path.Combine(logFolder, "collaborator.${date:format=yyyy-MM-dd}.log"),
            Layout = "${longdate} | ${level:uppercase=true} | ${logger} | ${message}${onexception:inner= | ${exception:format=Message}}",
            CreateDirs = true,
            MaxArchiveFiles = 7,
            ArchiveEvery = FileArchivePeriod.Day
        };

        config.AddTarget(fileTarget);
        config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, fileTarget);

        return config;
    }
}
