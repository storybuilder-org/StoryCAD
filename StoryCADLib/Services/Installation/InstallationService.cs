using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using StoryCAD.Models;
using StoryCAD.Services.Logging;

namespace StoryCAD.Services.Installation;

/// <summary>
/// This class copies the files from inside the executable to GlobalData.RootDirectory is.
/// </summary>
public class InstallationService
{
    public readonly LogService Logger;

    /// <summary>
    /// This copies all the files from \asetts\install to GlobalData.RootDirectory.
    /// This is done by using manifest resource steams and writing them to the disk.
    /// </summary>
    public async Task InstallFiles()
    {
        //Skip install files if version hasn't changed.
        if (GlobalData.Version == GlobalData.Preferences.Version)
        {
            return;
        }

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
            catch (Exception ex) { Logger.LogException(LogLevel.Error, ex, "Error in installer"); }
        }
    }

    /// <summary>
    /// This deletes all files in the parent directory EXCEPT StoryCAD.prf and all logs.
    /// </summary>
    private async Task DeleteFiles()
    {
        try
        {
            StorageFolder ParentFolder = await StorageFolder.GetFolderFromPathAsync(GlobalData.RootDirectory);
            if (ParentFolder != null)
            {
                ParentFolder = await StorageFolder.GetFolderFromPathAsync(Path.Combine(ApplicationData.Current.RoamingFolder.Path, "StoryCAD"));
            }

            foreach (StorageFile Item in await ParentFolder.GetFilesAsync())
            {
                if (Item.Name == "StoryCAD.prf") { continue; } //Don't delete prf

                try { await Item.DeleteAsync(); }
                catch (Exception ex) { Logger.LogException(LogLevel.Error, ex, "Error when deleting files"); }
            }
            foreach (StorageFolder Item in await ParentFolder.GetFoldersAsync())
            {
                if (Item.Name == "logs") { continue; }

                try { await Item.DeleteAsync(); }
                catch (Exception ex) { Logger.LogException(LogLevel.Error, ex, "Error when deleting files"); }
            }
        }
        catch (Exception ex)
        {
            Logger.LogException(LogLevel.Error,ex, "Error in deletefiles()");
        }

    }

    /// <summary>
    /// This processes the internal manifest resources into real file paths
    /// </summary>
    /// <returns></returns>
    private string ProcessFileName(string inputFileName)
    {
        Logger.Log(LogLevel.Trace, $"Processing file path of {inputFileName}");

        //Removes the parent container as the files are relative.
        string file = inputFileName.Replace("StoryCAD.Assets.Install", "");

        //This replaces all the . with \ (except the last . as that's the file type)
        int lastIndex = file.LastIndexOf('.');
        if (lastIndex > 0) { file = file[..lastIndex].Replace(".", @"\") + file[lastIndex..]; }
        file = file.Replace(@"\stbx", "stbx").Remove(0, 1).Replace("stbx", ".stbx").Replace("..", ".").Replace('_', ' ');

        //Fixes a dolls house name as all non ASCII (I think) characters are replaced with spaces except for the file name
        if (file.Contains("A Doll s House.stbx")) { file = file.Replace(" s ", "'s "); }

        //This fixes the UUID for samples.
        if (file.Contains("files"))
        {
            string[] path = file.Split("files");
            string UUID = path[1].Replace(" ", "-");
            file = path[0] + "files\\" + UUID[2..]; //Removes extra space in UUID
        }

        Logger.Log(LogLevel.Trace, $"Got ideal path as {file}");
        return file;
    }

    /// <summary>
    /// Creates empty placeholder files.
    /// </summary>
    /// <param name="File">The file.</param>
    /// <returns></returns>
    private async Task<StorageFile> CreateDummyFileAsync(string File)
    {
        StorageFolder ParentFolder = ApplicationData.Current.RoamingFolder;
        ParentFolder = await ParentFolder.CreateFolderAsync("StoryCAD", CreationCollisionOption.OpenIfExists);
        if (File.Contains(@"\"))
        {
            Logger.Log(LogLevel.Trace, $"{File} contains subdirectories");
            List<string> dirs = new(File.Split(@"\"));
            int iteration = 1;
            foreach (string dir in dirs)
            {
                if (iteration >= dirs.Count) { continue; }
                //Logger.Log(LogLevel.Trace, $"{File} mounting sub directory {dir}");
                ParentFolder = await ParentFolder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists);
                iteration++;
            }
        }

        return await ParentFolder.CreateFileAsync(Path.GetFileName(File), CreationCollisionOption.ReplaceExisting);
    }

    /// <summary>
    /// This opens the internal manifest data for a file and writes to the blank file made in CreateDummyFileAsync
    /// <param name="DiskFile"> File to be written to</param>
    /// <param name="InstallerFile"> manifest path ie StoryCAD.Assets.Install</param>
    /// </summary>
    private async void WriteManifestData(StorageFile DiskFile, string InstallerFile)
    {
        await using Stream internalResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(InstallerFile);
        await using (Stream fileDiskStream = await DiskFile.OpenStreamForWriteAsync())
        {
            Logger.Log(LogLevel.Trace, $"Opened manifest stream and stream for file on disk ({DiskFile.Path})");

            while (internalResourceStream.Position < internalResourceStream.Length)
            {
                fileDiskStream.WriteByte((byte)internalResourceStream.ReadByte());
            }
            await fileDiskStream.FlushAsync();
        }
        await internalResourceStream.FlushAsync();
    }

    public InstallationService() { Logger = Ioc.Default.GetService<LogService>(); }
}