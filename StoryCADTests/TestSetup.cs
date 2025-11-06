using CommunityToolkit.Mvvm.DependencyInjection;
using dotenv.net;
using StoryCADLib.Services;
using StoryCADLib.Services.Backup;
using StoryCADLib.Services.IoC;

namespace StoryCADTests;

/// <summary>
///     Global test setup and cleanup for all tests in the assembly.
/// </summary>
[TestClass]
public static class TestSetup
{
    /// <summary>
    ///     Runs once before any tests in the assembly.
    ///     Sets up IoC container and configures test environment.
    ///     DO NOT MODIFY THIS METHOD WITHOUT CONSULTING ALL TEAMS.
    /// </summary>
    [AssemblyInitialize]
    public static void Initialize(TestContext context)
    {
        BootStrapper.Initialise();
        var prefs = Ioc.Default.GetRequiredService<PreferenceService>();
        prefs.Model.ProjectDirectory = App.InputDir;
        prefs.Model.BackupDirectory = App.ResultsDir;

        // Disable all background services in tests to prevent timers from running
        prefs.Model.AutoSave = false;
        prefs.Model.TimedBackup = false;
        prefs.Model.BackupOnOpen = false;

        try
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
            DotEnvOptions options = new(false, [path]);
            DotEnv.Load(options);
        }
        catch
        {
        }
    }

    /// <summary>
    ///     Runs once after all tests in the assembly complete.
    ///     Stops and disposes services to ensure clean shutdown.
    /// </summary>
    [AssemblyCleanup]
    public static void Cleanup()
    {
        // Stop and dispose services to ensure timers don't keep running
        try
        {
            var autoSaveService = Ioc.Default.GetService<AutoSaveService>();
            if (autoSaveService != null)
            {
                autoSaveService.StopAutoSave();
                autoSaveService.Dispose();
            }

            var backupService = Ioc.Default.GetService<BackupService>();
            if (backupService != null)
            {
                backupService.StopTimedBackup();
            }
        }
        catch
        {
        }
    }
}
