using StoryCADLib.Services.MacFileAssociation;
using Uno.UI.Hosting;
namespace StoryCAD;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Install the kAEOpenDocuments handler before UNO creates NSApplication, so the
        // initial 'odoc' Apple Event on launch (Finder double-click, `open foo.stbx`) is
        // caught by us rather than NSApp's default NSDocument dispatch path (issue #963).
        if (OperatingSystem.IsMacOS())
        {
            MacFileOpenService.Initialize();
        }

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }
}
