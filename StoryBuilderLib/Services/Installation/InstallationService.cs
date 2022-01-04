using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
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


        /// <summary>
        /// This copies all the files from \assets\install to \RoamingState\StoryBuilder\
        /// via reading the manifest resource stream and writing them to the disk.
        /// </summary>
        /// <returns></returns>
        public async Task InstallFiles()
        {
            await DeleteFiles();
            foreach (string InstallerFile in Assembly.GetExecutingAssembly().GetManifestResourceNames())
            {
                try
                {
                    //This processes the internal path into one that is able to be written to the disk
                    string File = ProcessFileName(InstallerFile);
                    StorageFile DiskFile = await CreateDummyFileAsync(File);
                    WriteManifestData(DiskFile,InstallerFile);
                }
                catch (Exception ex)
                {
                    Logger.LogException(LogLevel.Error, ex, "error in new installer");
                }
            }
        }

        /// <summary>
        /// This deletes all files in the parent directory
        /// </summary>
        /// <returns></returns>
        private async Task DeleteFiles()
        {
            StorageFolder ParentFolder = await StorageFolder.GetFolderFromPathAsync(GlobalData.RootDirectory);
            foreach (StorageFile Item in await ParentFolder.GetFilesAsync())
            {
                if (Item.Name == "StoryBuilder.prf") { continue; } //Doesnt delete prf

                try { await Item.DeleteAsync(); }
                catch (Exception ex)
                {
                    Logger.LogException(LogLevel.Error, ex, "Error when deleting files");
                }

            }
            foreach (StorageFolder Item in await ParentFolder.GetFoldersAsync())
            {
                if (Item.Name == "logs") { continue; }

                try { await Item.DeleteAsync(); }
                catch (Exception ex)
                {
                    Logger.LogException(LogLevel.Error, ex, "Error when deleting files");
                }
            }
        }

        /// <summary>
        /// This processes the internal manifiest resources into propper file paths
        /// </summary>
        /// <returns></returns>
        private string ProcessFileName(string InputFileName)
        {
            Logger.Log(LogLevel.Trace, $"Processing file path of {InputFileName}");

            //Removes the parent container as the files are relative.
            string File = InputFileName.Replace("StoryBuilder.Assets.Install", "");

            //This replaces all the . with \ (except the last . as thats the file type)
            int lastIndex = File.LastIndexOf('.');
            if (lastIndex > 0) { File = File[..lastIndex].Replace(".", @"\") + File[lastIndex..]; }
            File = File.Replace(@"\stbx", "stbx").Remove(0, 1).Replace("stbx", ".stbx").Replace("..", ".").Replace('_', ' ');

            //Fixes a dolls house name as all non ASCII (I think) characters are replaced with spaces except for the file name
            if (File.Contains("A Doll s House.stbx")) { File = File.Replace(" s ", "'s "); }

            //This fixes the UUID for samples.
            if (File.Contains("files"))
            {
                string[] path = File.Split("files");
                string UUID = path[1].Replace(" ", "-");
                File = path[0] + "files\\" + UUID[2..]; //Removes extra space in UUID
            }

            Logger.Log(LogLevel.Trace, $"Got ideal path as {File}");
            return File;
        }

        //This traverses through 
        private async Task<StorageFile> CreateDummyFileAsync(string File)
        {
            StorageFolder ParentFolder = ApplicationData.Current.RoamingFolder;
            ParentFolder = await ParentFolder.CreateFolderAsync("StoryBuilder", CreationCollisionOption.OpenIfExists);
            if (File.Contains(@"\"))
            {
                Logger.Log(LogLevel.Trace, $"{File} contains subdirectories");
                List<string> dirs = new(File.Split(@"\"));
                int iteration = 1;
                foreach (string dir in dirs)
                {
                    if (iteration >= dirs.Count) { continue; }
                    Logger.Log(LogLevel.Trace, $"{File} mounting subdirectory {dir}");
                    ParentFolder = await ParentFolder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists);
                    iteration++;
                }
            }

            return await ParentFolder.CreateFileAsync(Path.GetFileName(File), CreationCollisionOption.ReplaceExisting);
        }

        /// <summary>
        /// This opens the internal manifiest data for a file and writes to the blank file made in CreateDummyFileAsync
        /// <param name="DiskFile"> File to be written to</param>
        /// <param name="InstallerFile"> manifest path eg StoryBUilder.Assets.Install</param>
        /// </summary>
        private async void WriteManifestData(StorageFile DiskFile, string InstallerFile)
        {
            List<Byte> ContentToWrite = new();
            await using Stream InternalResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(InstallerFile);
            await using (Stream FileDiskStream = await DiskFile.OpenStreamForWriteAsync())
            {
                Logger.Log(LogLevel.Trace, $"Opened manifest stream and stream for file on disk ({DiskFile.Path})");

                while (InternalResourceStream.Position < InternalResourceStream.Length)
                {
                    FileDiskStream.WriteByte((byte)InternalResourceStream.ReadByte());
                }
                await FileDiskStream.FlushAsync();
            }
            await InternalResourceStream.FlushAsync();
        }

        public InstallationService()
        {
            Logger = Ioc.Default.GetService<LogService>();
        }
    }
}
