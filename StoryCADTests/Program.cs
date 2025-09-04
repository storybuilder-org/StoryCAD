#if !WINDOWS
using System;

namespace StoryCADTests;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Console.WriteLine("StoryCAD Tests - Uno Platform");
        Console.WriteLine("Running tests on desktop platform...");
        
        // For desktop, we can run tests via dotnet test command
        // This main method is just to satisfy the executable requirement
        Console.WriteLine("Please run tests using: dotnet test");
    }
}
#endif