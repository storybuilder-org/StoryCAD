using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace StoryBuilder.DAL;

public static class StorageFolderExtensions
{
    /// <summary>
    /// Recursive copy of files and folders from source to destination.
    /// </summary>
    public static async Task CopyContentsRecursive(this IStorageFolder source, IStorageFolder dest)
    {
        await CopyContentsShallow(source, dest);

        IReadOnlyList<StorageFolder> subfolders = await source.GetFoldersAsync();
        foreach (StorageFolder storageFolder in subfolders)
        {
            await storageFolder.CopyContentsRecursive(await dest.GetFolderAsync(storageFolder.Name));
        }
    }

    /// <summary>
    /// Shallow copy of files and folders from source to destination.
    /// </summary>
    public static async Task CopyContentsShallow(this IStorageFolder source, IStorageFolder destination)
    {
        await source.CopyFiles(destination);

        IReadOnlyList<StorageFolder> items = await source.GetFoldersAsync();

        foreach (StorageFolder storageFolder in items)
        {
            await destination.CreateFolderAsync(storageFolder.Name, CreationCollisionOption.OpenIfExists);
        }
    }

    /// <summary>
    /// Copy files from source into destination folder.
    /// </summary>
    private static async Task CopyFiles(this IStorageFolder source, IStorageFolder destination)
    {
        IReadOnlyList<StorageFile> items = await source.GetFilesAsync();

        foreach (StorageFile storageFile in items)
        {
            await storageFile.CopyAsync(destination, storageFile.Name, NameCollisionOption.ReplaceExisting);
        }
    }
}