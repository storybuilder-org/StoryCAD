using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Services.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;

namespace StoryBuilder.Services.Installation
{
    /// <summary>
    /// InstallationService processes installation files for the
    /// application.
    ///
    /// This process)
    ///
    /// For now, I just install or replace the files.
    /// </summary>
    public class InstallationService
    {
        public readonly LogService Logger;

        private StorageFolder sourcelFolder;
        private StorageFile   installFile;
        private StorageFolder targetFolder;

        private IList<string> sourceManifest;
        private Dictionary<string, string> targetManifest;

        /// <summary>
        /// InstallFiles copies application assets, including control
        /// and tool initialization data, help files, report templates, 
        /// and sample story projects, to StoryBuilder's local application
        /// data folder. The entire contents of the application's \Assets\Install
        /// folder are copied from StoryBuilder app project.
        /// 
        /// In order to speed up the asset copying, InstallFiles file copies
        /// are driven from an install.manifest file, which lists the path
        /// of each file in the Assets\Install folder and its subfolders.
        /// Each file's SHA256 hash hash is compared to the same file's 
        /// hash from a copy of install.manifest copied to the local application
        /// data folder after all files are copied. The hashes match if the file
        /// hasn't changed, and it's skipped. If there's no install.manifest
        /// in %appdata%\install, all files are copied, along with install.manifest.
        /// 
        /// Note that the hash isn't used to verify file integrity, only to
        /// determine if the file has changed or not.
        /// </summary>
        /// <returns></returns>
        public async Task InstallFiles()
        {
            //if (true) { return; } //Uncomment this to skip the installer entirely
            string[] aa = Assembly.GetExecutingAssembly().GetManifestResourceNames();
            foreach (string InstallerFile in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                try
                {
                    //Turns manifiest path into a sensible one ready for storage
                    string File = InstallerFile.Replace("StoryBuilder.Assets.Install", "");

                    if (File.Contains("samp")) {
                        int a = 0; }
                    Logger.Log(LogLevel.Trace, $"Starting to install file {File}");
                    int lastIndex = File.LastIndexOf('.');
                    if (lastIndex > 0) { File = File[..lastIndex].Replace(".", @"\") + File[lastIndex..]; }
                    File = File.Replace(@"\stbx", "stbx").Remove(0, 1).Replace("stbx", ".stbx").Replace("..", ".").Replace('_', ' ');
                    Logger.Log(LogLevel.Trace, $"Got file path for manifest resource {InstallerFile} as {File}");

                    StorageFolder ParentFolder = ApplicationData.Current.RoamingFolder;
                    ParentFolder = await ParentFolder.CreateFolderAsync("StoryBuilder", CreationCollisionOption.OpenIfExists);
                    if (File.Contains(@"\"))
                    {
                        Logger.Log(LogLevel.Trace, $"{InstallerFile} contains subdirectories");
                        List<string> dirs = new(File.Split(@"\"));
                        int iteration = 1;
                        foreach (string dir in dirs)
                        {
                            if (iteration >= dirs.Count) { continue; }
                            Logger.Log(LogLevel.Trace, $"{InstallerFile} mounting subdirectory {dir}");
                            ParentFolder = await ParentFolder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists);
                            iteration++;
                        }
                    }

                    List<Byte> ContentToWrite = new();
                    using (Stream InternalResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(InstallerFile))
                    {
                        StorageFile DiskFile = await ParentFolder.CreateFileAsync(Path.GetFileName(File), CreationCollisionOption.ReplaceExisting);
                        using (Stream FileDiskStream = await DiskFile.OpenStreamForWriteAsync())
                        {
                            Logger.Log(LogLevel.Trace, $"Opened manifiest stream and stream for file on disk ({DiskFile.Path})");

                            while (InternalResourceStream.Position < InternalResourceStream.Length)
                            {
                                FileDiskStream.WriteByte((byte)InternalResourceStream.ReadByte());
                            }
                            await FileDiskStream.FlushAsync();
                        }
                        await InternalResourceStream.FlushAsync();
                    }
                    Logger.Log(LogLevel.Trace, $"Flushed stream for {File}");
                }
                catch (Exception ex)
                {
                    Logger.LogException(LogLevel.Error, ex, "error in new installer");
                }
            }
        }

        public InstallationService()
        {
            Logger = Ioc.Default.GetService<LogService>();
        }
    }
}
