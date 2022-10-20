using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Windows.Storage;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryBuilder.Models;
using StoryBuilder.Services.Logging;

namespace StoryBuilder.Services.Installation;

/// <summary>
/// This class copies the files from inside the executable to GlobalData.RootDirectory is.
/// </summary>
public class InstallationService
{
    public readonly LogService Logger;

    /// <summary>
    /// This copies all the files from \Assets\install to GlobalData.RootDirectory.
    /// This is done by using manifest resource steams and writing them to the disk.
    /// </summary>
    public async Task InstallFiles()
    {
        await DeleteFiles();
        foreach (string _InstallerFile in Assembly.GetExecutingAssembly().GetManifestResourceNames())
        {
            try
            {
                //This processes the internal path into one that is able to be written to the disk
                string _File = ProcessFileName(_InstallerFile);
                StorageFile _DiskFile = await CreateDummyFileAsync(_File);
                WriteManifestData(_DiskFile,_InstallerFile);
            }
            catch (Exception _Ex) { Logger.LogException(LogLevel.Error, _Ex, "Error in installer"); }
        }
    }

    /// <summary>
    /// This deletes all files in the parent directory EXCEPT StoryBuilder.prf and all logs.
    /// </summary>
    private async Task DeleteFiles()
    {
        StorageFolder _ParentFolder = await StorageFolder.GetFolderFromPathAsync(GlobalData.RootDirectory);
        foreach (StorageFile _Item in await _ParentFolder.GetFilesAsync())
        {
            if (_Item.Name == "StoryBuilder.prf") { continue; } //Don't delete prf

            try { await _Item.DeleteAsync(); }
            catch (Exception _Ex)
            {
                Logger.LogException(LogLevel.Error, _Ex, "Error when deleting files");
            }
        }
        foreach (StorageFolder _Item in await _ParentFolder.GetFoldersAsync())
        {
            if (_Item.Name == "logs") { continue; }

            try { await _Item.DeleteAsync(); }
            catch (Exception _Ex) { Logger.LogException(LogLevel.Error, _Ex, "Error when deleting files"); }
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
        string _File = inputFileName.Replace("StoryBuilder.Assets.Install", "");

        //This replaces all the . with \ (except the last . as that is the file type)
        int _LastIndex = _File.LastIndexOf('.');
        if (_LastIndex > 0) { _File = _File[.._LastIndex].Replace(".", @"\") + _File[_LastIndex..]; }
        _File = _File.Replace(@"\stbx", "stbx").Remove(0, 1).Replace("stbx", ".stbx").Replace("..", ".").Replace('_', ' ');

        //Fixes a dolls house name as all non ASCII (I think) characters are replaced with spaces except for the file name
        if (_File.Contains("A Doll s House.stbx")) { _File = _File.Replace(" s ", "'s "); }

        //This fixes the UUID for samples.
        if (_File.Contains("files"))
        {
            string[] _Path = _File.Split("files");
            string _Uuid = _Path[1].Replace(" ", "-");
            _File = _Path[0] + "files\\" + _Uuid[2..]; //Removes extra space in UUID
        }

        Logger.Log(LogLevel.Trace, $"Got ideal path as {_File}");
        return _File;
    }

    /// <summary>
    /// Creates empty placeholder files.
    /// </summary>
    /// <param name="file">The file.</param>
    /// <returns></returns>
    private async Task<StorageFile> CreateDummyFileAsync(string file)
    {
        StorageFolder _ParentFolder = ApplicationData.Current.RoamingFolder;
        _ParentFolder = await _ParentFolder.CreateFolderAsync("StoryBuilder", CreationCollisionOption.OpenIfExists);
        if (file.Contains(@"\"))
        {
            Logger.Log(LogLevel.Trace, $"{file} contains subdirectories");
            List<string> _Dirs = new(file.Split(@"\"));
            int _Iteration = 1;
            foreach (string _Dir in _Dirs)
            {
                if (_Iteration >= _Dirs.Count) { continue; }
                //Logger.Log(LogLevel.Trace, $"{File} mounting sub directory {dir}");
                _ParentFolder = await _ParentFolder.CreateFolderAsync(_Dir, CreationCollisionOption.OpenIfExists);
                _Iteration++;
            }
        }

        return await _ParentFolder.CreateFileAsync(Path.GetFileName(file), CreationCollisionOption.ReplaceExisting);
    }

    /// <summary>
    /// This opens the internal manifest data for a file and writes to the blank file made in CreateDummyFileAsync
    /// <param name="diskFile"> File to be written to</param>
    /// <param name="installerFile"> manifest path ie StoryBuilder.Assets.Install</param>
    /// </summary>
    private async void WriteManifestData(StorageFile diskFile, string installerFile)
    {
        await using Stream _InternalResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(installerFile);
        await using (Stream _FileDiskStream = await diskFile.OpenStreamForWriteAsync())
        {
            Logger.Log(LogLevel.Trace, $"Opened manifest stream and stream for file on disk ({diskFile.Path})");

            while (_InternalResourceStream!.Position < _InternalResourceStream.Length)
            {
                _FileDiskStream.WriteByte((byte)_InternalResourceStream.ReadByte());
            }
            await _FileDiskStream.FlushAsync();
        }
        await _InternalResourceStream.FlushAsync();
    }

    public InstallationService() { Logger = Ioc.Default.GetService<LogService>(); }
}