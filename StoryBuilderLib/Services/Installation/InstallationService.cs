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
            try
            {
                //TODO: Log Installation

                // Get the target (%appdata%\StoryBuilder) folder
                string targetPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}";
                targetFolder = await StorageFolder.GetFolderFromPathAsync(targetPath);
                // If there is no %appdata/StoryBuilder folder, create one
                targetFolder = await targetFolder.CreateFolderAsync(@"StoryBuilder", CreationCollisionOption.OpenIfExists);
                
                // Get the source (\Assets\Install) path from the executing program's location
                string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                int i = assemblyPath.IndexOf(@"\StoryBuilder\StoryBuilder");
                string installPath = assemblyPath.Substring(0, i) + @"\StoryBuilder\StoryBuilder\StoryBuilder";
                installPath = Path.Combine(installPath, "Assets", "Install");
                sourcelFolder = await StorageFolder.GetFolderFromPathAsync(installPath);

                // Read the new and old install.manifest files into memory
                await ReadTargetManifest();
                await ReadSourceManifest();

                bool changed = false;

                // Process each line in the source (\Assets\Install) install.manifest 
                // The install.manifest is is created by the CreateInstallManifest
                // (in this solution) and is in the format:
                //     filename,hash
                // where filename may also have a folder prefix. For example:
                // Bibliog.txt,29483b0fdf7550d3e2c2c54a11e4fbf5d53ed0f4b11983c6d0e36b2cd209a1bb
                // or
                // samples\Hamlet.stbx\Hamlet.stbx,7d5c11d7d8a5d42a9e1d005545182babb9616213769393fa8de274166f3edcf9
                foreach (string manifestEntry in sourceManifest)
                {
                    string[] tokens = manifestEntry.Split(',');
                    string fileName = tokens[0];
                    string sourceHash = tokens[1];

                    // Skip the manifest itself until after the other copies
                    if (fileName.Equals("install.manifest"))
                        continue;

                    // The target manifest was loaded into a dictionary
                    // [filename],[hash] for lookup. If the file isn't
                    // in the target manifest, it needs added.
                    if (!targetManifest.ContainsKey(fileName))
                    {
                        string msg = $"Adding installation file {fileName}";
                        Logger.Log(LogLevel.Info, msg);
                        await InstallFileAsync(fileName);
                        changed = true;
                        continue;
                    }
                    // If it's in the target manifest, see if it's changed
                    if (!sourceHash.Equals(targetManifest[fileName]))
                    {
                        string msg = $"Replacing installation file {fileName} - hash changed";
                        Logger.Log(LogLevel.Info, msg);
                        await InstallFileAsync(fileName);
                        changed = true;
                    }
                }

                if (changed)
                {
                    string msg = $"Installing local copy of install.manifest";
                    Logger.Log(LogLevel.Info, msg);
                    // copy install manifest as local manifest
                    await installFile.CopyAsync(targetFolder, "install.manifest", NameCollisionOption.ReplaceExisting);
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(LogLevel.Error, ex, "Error in InstallFiles");
            }
        }

        private async Task InstallFileAsync(string fileName)
        {
            string installPath = Path.Combine(sourcelFolder.Path, fileName);
            StorageFile file = await StorageFile.GetFileFromPathAsync(installPath);

            StorageFolder dest = await GetTargetlFolder(fileName);
            string target = Path.GetFileName(fileName);
            string msg = $"Copying file {target} to {dest.Path}";
            Logger.Log(LogLevel.Info, msg);
            await file.CopyAsync(dest, target, NameCollisionOption.ReplaceExisting);
            if (target.EndsWith("stbx"))  // if it's a sample file, add a "files" subfolder
            {
                if (await dest.TryGetItemAsync("files") == null)
                {
                    msg = $"Adding files subfolder to {dest.Path}";
                    Logger.Log(LogLevel.Info, msg);
                    await dest.CreateFolderAsync("files");
                }
            }
        }

        /// <summary>
        /// Find or create the destination folder.
        /// 
        /// The argument can contain zero or more subfolders. This routine
        /// loops through them in turn until the complete destination folder
        /// is found, creating any missing folders along the way.
        /// </summary>
        /// <param name="fileName">Relative path excluding filename and extension</param>
        /// <returns>StorageFile</returns>
        private async Task<StorageFolder> GetTargetlFolder(string fileName)
        {
            StorageFolder destination = targetFolder;
            string folders = Path.GetDirectoryName(fileName);
            if (folders.Equals(string.Empty))
                return destination;
            string[] nodes = folders.Split(Path.DirectorySeparatorChar);
            // There are subfolders 
            foreach (string node in nodes)
            {
                 destination = await destination.CreateFolderAsync(node,CreationCollisionOption.OpenIfExists);
            }
            return destination;
        }

        private async Task ReadTargetManifest()
        {
            installFile = await sourcelFolder.GetFileAsync("install.manifest");
            sourceManifest = await FileIO.ReadLinesAsync(installFile);
        }

        private async Task ReadSourceManifest()
        {
            targetManifest = new Dictionary<string, string>();
            IList<string> localEntries;
            IStorageFile file = await targetFolder.TryGetItemAsync("install.manifest") as IStorageFile;
            if (file == null)  // If there is no manifest return with empty Dictionary
                return;
            localEntries = await FileIO.ReadLinesAsync(file);
            foreach (string line in localEntries)
            {
                string[] tokens = line.Split(',');
                targetManifest.Add(tokens[0], tokens[1]);
            }
        }

        public InstallationService()
        {
            Logger = Ioc.Default.GetService<LogService>();
        }
    }
}
