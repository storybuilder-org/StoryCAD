using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Toolkit.Mvvm.DependencyInjection;
using StoryBuilder.Services.Logging;
using Windows.Storage;

namespace StoryBuilder.Services.Installation
{
    /// <summary>
    /// InstallationService processes installation files for the
    /// application.
    ///
    /// Properly this will also update newer installation files
    /// (using a manifest and checksums, for instance.)
    ///
    /// For now, I just install or replace the files.
    /// </summary>
    public class InstallationService
    {
        public readonly LogService Logger;

        private StorageFolder _installFolder;
        private StorageFile _installFile;
        private StorageFolder _localFolder;
        private IList<string> _installManifest;
        private Dictionary<string, string> _localManifest;

        /// <summary>
        /// InstallFiles copies application assets, including control
        /// and tool initialization data, help files, report templates, 
        /// and sample story projects, to StoryBuilder's local application
        /// data folder. The entire contents of the application's Assets
        /// folder are copied from the msix bundle. Although this method
        /// runs during code initialization, it's a deferred installation
        /// process.
        /// 
        /// In order to speed up the asset copying, InstallFiles file copies
        /// are driven from an install.manifest file, which lists the path
        /// of each file in the Assets folder and its subfolders, and that
        /// file's SHA256 hash. Each file's hash is compared to the same file's 
        /// hash from a copy of install.manifest copied to the local application
        /// data folder after all files are copied. The hashes match the file
        /// hasn't changed, and it isn't copied. If there's no local folder 
        /// install.manifest, all files are copied, along with install.manifest.
        /// 
        /// Note that the hash isn't used to verify file integrity, only to
        /// determine if the file has changed or not.
        /// </summary>
        /// <returns></returns>
        public async Task InstallFiles()
        { 
            //TODO: Log Installation
            _installFolder = await Windows.ApplicationModel.Package.Current.InstalledLocation.GetFolderAsync(@"Assets\Install");
            _localFolder = ApplicationData.Current.LocalFolder;
            await ReadInstallManifest();
            await ReadLocalManifest();
            bool changed = false;
            foreach (string manifestEntry in _installManifest)
            {
                string[] tokens = manifestEntry.Split(',');
                if (!_localManifest.ContainsKey(tokens[0]))
                {
                    string msg = $"Adding installation file {tokens[0]}";
                    Logger.Log(LogLevel.Info, msg);
                    await InstallFileAsync(tokens[0]);
                    changed = true;
                    continue;
                }
                string localHash = _localManifest[tokens[0]];
                string installHash = tokens[1];
                if (!installHash.Equals(localHash))
                {
                    string msg = $"Replacing installation file {tokens[0]} - hash changed";
                    Logger.Log(LogLevel.Info, msg);
                    await InstallFileAsync(tokens[0]);
                    changed = true;
                }
            }
            if (changed)
            {
                string msg = $"Installing local copy of install.manifest";
                Logger.Log(LogLevel.Info, msg);
                // copy install manifest as local manifest
                await _installFile.CopyAsync(_localFolder, "install.manifest", NameCollisionOption.ReplaceExisting);
            }
        }

        private async Task InstallFileAsync(string path)
        {
            StorageFile file = await StorageFile.GetFileFromPathAsync(path);
            string fileName = Path.GetFileName(path);
            int i = path.IndexOf(@"\Assets\Install");
            string relPath = Path.GetDirectoryName(path.Substring(i + 16));
            StorageFolder dest = await InstallFolder(relPath);
            string msg = $"Copying file {fileName} to {dest.Path}";
            Logger.Log(LogLevel.Info, msg);
            await file.CopyAsync(dest, fileName, NameCollisionOption.ReplaceExisting);
            if (path.EndsWith("stbx"))  // if it's a sample file, add a "files" subfolder
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
        /// <param name="subPath">Relative path excluding filename and extension</param>
        /// <returns>StorageFile</returns>
        private async Task<StorageFolder> InstallFolder(string subPath)
        {
            StorageFolder dest = _localFolder;
            if (subPath.Equals(string.Empty))
                return dest;
            string[] nodes = subPath.Split(Path.DirectorySeparatorChar);
            foreach (string node in nodes) 
            {
                if (await dest.TryGetItemAsync(node) == null)
                    dest = await dest.CreateFolderAsync(node);
                else
                    dest = await dest.GetFolderAsync(node);
            }
            return dest;
        }

        private async Task ReadInstallManifest()
        {
            _installFile = await _installFolder.GetFileAsync("install.manifest");
            _installManifest = await FileIO.ReadLinesAsync(_installFile);
        }

        private async Task ReadLocalManifest()
        {
            _localManifest = new Dictionary<string, string>();
            IList<string> localEntries;
            IStorageFile file = await _localFolder.TryGetItemAsync("install.manifest") as IStorageFile;
            if (file == null)  // If there is no manifest return with empty Dictionary
               return;
            localEntries = await FileIO.ReadLinesAsync(file);
            foreach (string line in localEntries)
            {
                string[] tokens = line.Split(',');
                _localManifest.Add(tokens[0], tokens[1]);
            }
        }

        public InstallationService()
        {
            Logger = Ioc.Default.GetService<LogService>();
        }
    }
}
