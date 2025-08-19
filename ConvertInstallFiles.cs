using System;
using System.IO;
using StoryCAD.Utilities;

// Simple conversion script to convert INI files to JSON
class ConvertInstallFiles
{
    static void Main()
    {
        var basePath = @"C:\Users\Jake\Documents\GitHub\StoryCAD\StoryCADLib\Assets\Install";
        
        try
        {
            // Convert Lists.ini
            Console.WriteLine("Converting Lists.ini to Lists.json...");
            IniToJsonConverter.ConvertListsIni(
                Path.Combine(basePath, "Lists.ini"),
                Path.Combine(basePath, "Lists.json")
            );
            Console.WriteLine("Lists.json created successfully!");
            
            // Convert Tools.ini
            Console.WriteLine("Converting Tools.ini to Tools.json...");
            IniToJsonConverter.ConvertToolsIni(
                Path.Combine(basePath, "Tools.ini"),
                Path.Combine(basePath, "Tools.json")
            );
            Console.WriteLine("Tools.json created successfully!");
            
            Console.WriteLine("\nConversion complete!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during conversion: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
}