namespace StoryCADAutomation;

/// <summary>
///     Entry point for the StoryCAD UI automation runner (issue #1421). Scaffold only:
///     subcommand dispatch and the exit-code contract are wired; the run and check
///     implementations arrive with the driver, parser, and interpreter tasks.
/// </summary>
internal static class Program
{
    // Exit-code contract per devdocs/issue_1421_dsl_design.md "Runner": 0 all steps passed,
    // 1 step failure(s), 2 script parse/lint error, 3 environment or launch failure.
    private static int Main(string[] args)
    {
        if (args.Length > 0 && args[0] is "run" or "check")
        {
            Console.Error.WriteLine($"'{args[0]}' is not implemented yet; scaffold only (issue #1421).");
            return 2;
        }

        Console.Error.WriteLine("StoryCAD automation runner (issue #1421)");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Usage:");
        Console.Error.WriteLine("  StoryCADAutomation run <script.scs | directory> [options]");
        Console.Error.WriteLine("  StoryCADAutomation check <script.scs | directory>");
        Console.Error.WriteLine();
        Console.Error.WriteLine("Exit codes: 0 all steps passed, 1 step failure(s), 2 script parse/lint error,");
        Console.Error.WriteLine("            3 environment or launch failure.");
        return 2;
    }
}
