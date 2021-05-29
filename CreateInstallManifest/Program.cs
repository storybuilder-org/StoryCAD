using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using DirectoryComparer.Services;


namespace CreateInstallManifest
{
    class Program
    {
        /// <summary>
        /// Purpose of Program:
        /// This console application reads the contents of the StoryBuilder application's \Assets\Install folder
        /// and it's child folders and produces a text document containing the file's path names and SHA256
        /// hashes. The list is written to the \Assets\Install folder as 'Install.manifest'. 
        /// 
        /// At the beginning of StoryBuilder's OnLaunched event in App.xaml.cs, ProcessInstallationFiles()
        /// bootstraps StoryBuilder's data (help, reporting templates, control contents, etc.) to the
        /// application's current installation location. There's a fair amount of data and this slows 
        /// startup. The manifest is compared to to one stored in the application's current installation location
        /// in order to avoid re-installing files that are already copied and unchanged, and thus speed
        /// up the load.
        /// 
        /// Usage:
        /// CreateInstallManifest runs as a post-build event during the StoryBuilder project build. The
        /// command line is as follows:
        ///     $(SolutionDir)\CreateInstallManifest.exe  
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            // The \StoryBuilder\StoryBuilder\StoryBuilder\Assets\Install\ folder path
            // is passed to the program
            var installFolder = args[0];
            Debug.WriteLine(installFolder);
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
                manifest.WriteLine("{0},{1}",file,hash);
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
