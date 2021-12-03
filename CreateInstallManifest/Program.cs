using DirectoryComparer.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;


namespace CreateInstallManifest
{
    class Program
    {
        /// <summary>
        /// Purpose of Program:
        /// This console application reads the contents of the StoryBuilder application's \Assets\Install folder
        /// and it's child folders and produces a text document containing the file's relative path names and SHA256
        /// hashes. The list is written into the same \Assets\Install folder as 'install.manifest'. 
        /// 
        /// At the beginning of StoryBuilder's OnLaunched event in App.xaml.cs, ProcessInstallationFiles()
        /// bootstraps StoryBuilder's data (help, reporting templates, control contents, etc.) to the
        /// application's current installation location, %appdata%\StoryBuilder. There's a fair amount of data and this slows 
        /// startup. The manifest is compared to to the one stored in the application's current installation location
        /// in order to avoid re-installing files that are already copied and unchanged, and thus speed up the load.
        /// Since the copy will copy install.manifest as well, it only updates for added, deleted, and changed files.
        /// 
        /// Usage:
        /// Right click CreateInstallmanifest and select "Set as Startup Project". Then start and run the program.
        /// CreateInstallManifest must run from within the Visual Studio solution to find, read and update the
        /// StoryBuilder\Assets\Install folder.
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // find the \Assets\install folder
            string assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
            int i = assemblyLocation.IndexOf("StoryBuilder");
            string path = assemblyLocation.Substring(0, i);
            string installFolder = Path.Combine(path, "StoryBuilder", "StoryBuilder","Assets", "Install");

            Debug.WriteLine(installFolder);
            int baseLength = installFolder.Length;
            // Get a List of all files in the folder and its subfolders
            List<string> files = DirectoryLister.GetAllFiles(installFolder, true);

            HashAlgorithm hasher = new SHA256CryptoServiceProvider();
            // Open the output document
            string destPath = Path.Combine(installFolder, "install.manifest");
            if (File.Exists(destPath))
                File.Delete(destPath);
            using StreamWriter manifest = new StreamWriter(destPath);
            foreach (string file in files)
            {
                string hash = GetHash(file, hasher);
                string relativePath = file.Substring(installFolder.Length + 1);
                manifest.WriteLine("{0},{1}", relativePath, hash);
            }
            manifest.Close();
        }

        private static string GetHash(string filePath, HashAlgorithm hasher)
        {
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                return GetHash(fs, hasher);
        }
        private static string GetHash(Stream s, HashAlgorithm hasher)
        {
            var hash = hasher.ComputeHash(s);
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }
}
