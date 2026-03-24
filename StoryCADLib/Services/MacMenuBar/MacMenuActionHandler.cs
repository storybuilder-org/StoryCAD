using System.Runtime.InteropServices;
using StoryCADLib.Services.Logging;

namespace StoryCADLib.Services.MacMenuBar;

/// <summary>
/// Bridges NSMenuItem target-action callbacks to managed C# delegates.
/// Dynamically registers an ObjC class at runtime that receives menu item actions
/// and dispatches them to the corresponding ShellViewModel commands.
/// </summary>
internal class MacMenuActionHandler
{
    private readonly ILogService _logger;

    /// <summary>
    /// The ObjC class registered at runtime to receive menu actions.
    /// </summary>
    private IntPtr _handlerClass;

    /// <summary>
    /// A single instance of the handler class used as the target for all menu items.
    /// </summary>
    internal IntPtr Instance { get; private set; }

    /// <summary>
    /// Prevents GC from collecting the delegates passed to class_addMethod.
    /// </summary>
    private static readonly List<Delegate> PinnedDelegates = new();

    /// <summary>
    /// Maps selector names to the Action to invoke when that selector fires.
    /// </summary>
    private readonly Dictionary<string, Action> _selectorActions = new();

    internal MacMenuActionHandler(ILogService logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Creates the ObjC handler class and allocates an instance.
    /// Call this once during initialization.
    /// </summary>
    internal void Initialize()
    {
        if (!OperatingSystem.IsMacOS()) return;

        IntPtr nsObjectClass = ObjCRuntime.objc_getClass("NSObject");
        _handlerClass = ObjCRuntime.objc_allocateClassPair(nsObjectClass, "StoryCADMenuHandler", 0);
        if (_handlerClass == IntPtr.Zero)
        {
            // Class may already exist from a previous initialization (hot reload scenario)
            _handlerClass = ObjCRuntime.objc_getClass("StoryCADMenuHandler");
            if (_handlerClass == IntPtr.Zero)
            {
                _logger.Log(LogLevel.Error, "Failed to create or find StoryCADMenuHandler ObjC class");
                return;
            }
        }
        else
        {
            ObjCRuntime.objc_registerClassPair(_handlerClass);
        }

        // Allocate and init an instance
        IntPtr alloc = ObjCRuntime.objc_msgSend(_handlerClass, ObjCRuntime.sel_registerName("alloc"));
        Instance = ObjCRuntime.objc_msgSend(alloc, ObjCRuntime.sel_registerName("init"));
    }

    /// <summary>
    /// Registers a menu action: adds an ObjC method to the handler class for the given selector,
    /// and maps it to the provided C# action.
    /// </summary>
    /// <param name="selectorName">ObjC selector name, e.g. "openFile:"</param>
    /// <param name="action">The C# action to invoke (typically command.Execute(null))</param>
    /// <returns>The registered selector IntPtr for use with NSMenuItem</returns>
    internal IntPtr RegisterAction(string selectorName, Action action)
    {
        if (!OperatingSystem.IsMacOS()) return IntPtr.Zero;

        _selectorActions[selectorName] = action;

        IntPtr sel = ObjCRuntime.sel_registerName(selectorName);

        // Create the native callback delegate
        ObjCRuntime.ActionDelegate callback = (self, selector, sender) =>
        {
            try
            {
                if (_selectorActions.TryGetValue(selectorName, out var act))
                {
                    act();
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, $"MacMenuBar action '{selectorName}' failed: {ex.Message}");
            }
        };

        // Pin the delegate to prevent GC collection
        PinnedDelegates.Add(callback);

        // Get the function pointer and add the method to the ObjC class
        IntPtr funcPtr = Marshal.GetFunctionPointerForDelegate(callback);
        // "v@:@" = void return, self, selector, sender (all object pointers)
        ObjCRuntime.class_addMethod(_handlerClass, sel, funcPtr, "v@:@");

        return sel;
    }
}
