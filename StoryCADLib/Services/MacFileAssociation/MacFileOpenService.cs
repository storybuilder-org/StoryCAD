using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.DependencyInjection;
using StoryCADLib.Models;
using StoryCADLib.Services.Logging;
using StoryCADLib.Services.MacInterop;
using StoryCADLib.ViewModels;

namespace StoryCADLib.Services.MacFileAssociation;

/// <summary>
/// Receives macOS "Open Documents" Apple Events so that .stbx files double-clicked in
/// Finder, dropped on the Dock icon, or opened via <c>open foo.stbx</c> are routed into
/// StoryCAD. Installs a handler on NSAppleEventManager for kCoreEventClass/kAEOpenDocuments
/// (<c>'aevt'/'odoc'</c>).
///
/// Timing detail: NSApplication.finishLaunching installs its OWN kAEOpenDocuments handler
/// and then processes queued Apple Events — which means any handler we register in
/// Program.Main is overwritten before the launch event dispatches. To win the race we
/// also observe <c>NSApplicationWillFinishLaunchingNotification</c>, which fires inside
/// finishLaunching AFTER NSApp installs its default handler but BEFORE the queued event
/// processing; our observer re-installs our handler at that exact moment.
/// </summary>
public static class MacFileOpenService
{
    // Keeps callback delegates alive for the lifetime of the process. class_addMethod and
    // notification observers store only function pointers — a collected delegate would
    // produce a jump into freed memory when Cocoa next calls through.
    private static readonly List<Delegate> PinnedDelegates = new();

    private static IntPtr _handlerClass;
    private static IntPtr _handlerInstance;
    private static bool _initialized;

    // Event may fire during NSApplication.finishLaunching — before App() ctor runs and
    // BootStrapper.Initialise has configured IoC. We can't resolve ShellViewModel yet,
    // so stash the path here and let startup drain it once IoC is up.
    private static readonly object PendingLock = new();
    private static string _pendingPath;

    /// <summary>
    /// Returns and clears the file path (if any) received from a file-open Apple Event
    /// that arrived before the app was ready to route it. Called by startup code once
    /// IoC and ShellViewModel are available.
    /// </summary>
    public static string ConsumePendingPath()
    {
        lock (PendingLock)
        {
            var p = _pendingPath;
            _pendingPath = null;
            return p;
        }
    }

    // kCoreEventClass = 'aevt', kAEOpenDocuments = 'odoc', keyDirectObject = '----'
    private static readonly uint KCoreEventClass = ObjCRuntime.FourCharCode("aevt");
    private static readonly uint KAEOpenDocuments = ObjCRuntime.FourCharCode("odoc");
    private static readonly uint KeyDirectObject = ObjCRuntime.FourCharCode("----");

    // -(void)handleOpenDocumentsEvent:(NSAppleEventDescriptor*)ev withReplyEvent:(NSAppleEventDescriptor*)reply
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void AppleEventDelegate(IntPtr self, IntPtr selector, IntPtr eventDesc, IntPtr replyDesc);

    // -(void)applicationWillFinishLaunching:(NSNotification*)n
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void NotificationDelegate(IntPtr self, IntPtr selector, IntPtr notification);

    /// <summary>
    /// Installs the Apple Event handler. Idempotent and a no-op on non-macOS platforms.
    /// Safe to call from Program.Main before IoC is configured — does not touch IoC.
    /// </summary>
    public static void Initialize()
    {
        if (!OperatingSystem.IsMacOS() || _initialized) return;

        try
        {
            ObjCRuntime.LoadCocoa();
            RegisterHandlerClass();
            InstallAppleEventHandler();       // may be overwritten by NSApp.finishLaunching
            InstallWillFinishLaunchingObserver(); // re-installs us after NSApp's overwrite
            _initialized = true;
        }
        catch (Exception ex)
        {
            // IoC may not exist yet — emit to stderr so the error isn't swallowed.
            Console.Error.WriteLine($"MacFileOpenService.Initialize failed: {ex}");
        }
    }

    private static void RegisterHandlerClass()
    {
        IntPtr nsObject = ObjCRuntime.objc_getClass("NSObject");
        _handlerClass = ObjCRuntime.objc_allocateClassPair(nsObject, "StoryCADAppleEventHandler", 0);
        if (_handlerClass == IntPtr.Zero)
        {
            // Class already registered in this process (hot reload) — fetch the existing one.
            _handlerClass = ObjCRuntime.objc_getClass("StoryCADAppleEventHandler");
            if (_handlerClass == IntPtr.Zero)
                throw new InvalidOperationException("Could not create or find StoryCADAppleEventHandler class.");
        }
        else
        {
            AppleEventDelegate aeThunk = HandleOpenDocuments;
            PinnedDelegates.Add(aeThunk);
            IntPtr aeSel = ObjCRuntime.sel_registerName("handleOpenDocumentsEvent:withReplyEvent:");
            // "v@:@@" — void; self (@); _cmd (:); event desc (@); reply desc (@)
            if (!ObjCRuntime.class_addMethod(_handlerClass, aeSel,
                    Marshal.GetFunctionPointerForDelegate(aeThunk), "v@:@@"))
                throw new InvalidOperationException("class_addMethod failed for handleOpenDocumentsEvent:withReplyEvent:");

            NotificationDelegate nThunk = OnApplicationWillFinishLaunching;
            PinnedDelegates.Add(nThunk);
            IntPtr nSel = ObjCRuntime.sel_registerName("applicationWillFinishLaunching:");
            // "v@:@" — void; self; _cmd; notification
            if (!ObjCRuntime.class_addMethod(_handlerClass, nSel,
                    Marshal.GetFunctionPointerForDelegate(nThunk), "v@:@"))
                throw new InvalidOperationException("class_addMethod failed for applicationWillFinishLaunching:");

            ObjCRuntime.objc_registerClassPair(_handlerClass);
        }

        IntPtr alloc = ObjCRuntime.objc_msgSend(_handlerClass, ObjCRuntime.sel_registerName("alloc"));
        _handlerInstance = ObjCRuntime.objc_msgSend(alloc, ObjCRuntime.sel_registerName("init"));
    }

    private static void InstallAppleEventHandler()
    {
        IntPtr aemClass = ObjCRuntime.objc_getClass("NSAppleEventManager");
        IntPtr aem = ObjCRuntime.objc_msgSend(aemClass, ObjCRuntime.sel_registerName("sharedAppleEventManager"));

        IntPtr setEventHandler = ObjCRuntime.sel_registerName(
            "setEventHandler:andSelector:forEventClass:andEventID:");
        IntPtr handleSel = ObjCRuntime.sel_registerName("handleOpenDocumentsEvent:withReplyEvent:");

        ObjCRuntime.objc_msgSend_setEventHandler(
            aem, setEventHandler,
            _handlerInstance, handleSel,
            KCoreEventClass, KAEOpenDocuments);
    }

    private static void InstallWillFinishLaunchingObserver()
    {
        // [NSNotificationCenter defaultCenter] addObserver:_handlerInstance
        //     selector:@selector(applicationWillFinishLaunching:)
        //     name:@"NSApplicationWillFinishLaunchingNotification" object:nil
        IntPtr ncClass = ObjCRuntime.objc_getClass("NSNotificationCenter");
        IntPtr center = ObjCRuntime.objc_msgSend(ncClass, ObjCRuntime.sel_registerName("defaultCenter"));

        IntPtr addObserver = ObjCRuntime.sel_registerName("addObserver:selector:name:object:");
        IntPtr sel = ObjCRuntime.sel_registerName("applicationWillFinishLaunching:");
        IntPtr name = ObjCRuntime.CreateNSString("NSApplicationWillFinishLaunchingNotification");

        // addObserver:selector:name:object: — 4 object/pointer args, void return.
        ObjCRuntime.objc_msgSend(center, addObserver, _handlerInstance, sel, name, IntPtr.Zero);
    }

    // Fires inside NSApplication.finishLaunching after NSApp has installed its default
    // Apple Event handlers but before the queued 'odoc' event is processed. This is our
    // window to re-install our handler and win the race.
    private static void OnApplicationWillFinishLaunching(IntPtr self, IntPtr selector, IntPtr notification)
    {
        try
        {
            InstallAppleEventHandler();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Reinstalling AE handler failed: {ex}");
        }
    }

    // Runs on the main thread when NSApplication dispatches the Apple Event. Exceptions must
    // not escape this method — they would unwind into Cocoa's dispatch loop and kill the app.
    // This may fire during NSApplication.finishLaunching, i.e. before App() ctor and IoC,
    // so DO NOT touch IoC directly from here; stash the path and let startup drain it.
    private static void HandleOpenDocuments(IntPtr self, IntPtr selector, IntPtr eventDesc, IntPtr replyDesc)
    {
        try
        {
            var paths = ExtractPaths(eventDesc);
            if (paths.Count == 0) return;

            string path = paths[0];

            // Try the fast path (running app): if IoC is already configured and Shell is loaded,
            // route directly. Otherwise stash for startup drain.
            if (!TryRouteImmediately(path))
            {
                lock (PendingLock) _pendingPath = path;
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"HandleOpenDocuments failed: {ex}");
        }
    }

    private static bool TryRouteImmediately(string path)
    {
        try
        {
            var shellVm = Ioc.Default.GetService<ShellViewModel>();
            if (shellVm == null) return false; // IoC not ready — caller will stash

            var windowing = Ioc.Default.GetService<Windowing>();
            if (windowing?.GlobalDispatcher != null)
            {
                // Shell is loaded — open on the UI thread now.
                windowing.GlobalDispatcher.TryEnqueue(async () =>
                {
                    try { await shellVm.OutlineManager.OpenFile(path); }
                    catch (Exception ex)
                    {
                        Ioc.Default.GetService<ILogService>()
                            ?.LogException(LogLevel.Error, ex, $"OpenFile failed for {path}");
                    }
                });
            }
            else
            {
                // IoC is up but Shell hasn't loaded yet — let Shell_Loaded pick it up.
                shellVm.FilePathToLaunch = path;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<string> ExtractPaths(IntPtr eventDesc)
    {
        var result = new List<string>();
        if (eventDesc == IntPtr.Zero) return result;

        IntPtr paramForKeyword = ObjCRuntime.sel_registerName("paramDescriptorForKeyword:");
        IntPtr list = ObjCRuntime.objc_msgSend_fourcc(eventDesc, paramForKeyword, KeyDirectObject);
        if (list == IntPtr.Zero) return result;

        int count = ObjCRuntime.objc_msgSend_int(list, ObjCRuntime.sel_registerName("numberOfItems"));
        if (count <= 0) return result;

        IntPtr descriptorAtIndex = ObjCRuntime.sel_registerName("descriptorAtIndex:");
        IntPtr fileURLValue = ObjCRuntime.sel_registerName("fileURLValue");

        // AEDesc lists are 1-indexed.
        for (long i = 1; i <= count; i++)
        {
            IntPtr item = ObjCRuntime.objc_msgSend_index(list, descriptorAtIndex, i);
            if (item == IntPtr.Zero) continue;

            IntPtr url = ObjCRuntime.objc_msgSend(item, fileURLValue);
            string path = ObjCRuntime.NSUrlToPath(url);
            if (!string.IsNullOrEmpty(path))
                result.Add(path);
        }

        return result;
    }

}
