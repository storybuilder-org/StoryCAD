using System.Runtime.InteropServices;

namespace StoryCADLib.Services.MacInterop;

/// <summary>
/// Low-level P/Invoke declarations for the Objective-C runtime on macOS.
/// Shared primitive consumed by the macOS menu bar, print dialog, and file-association
/// services. All public entry points are guarded by OperatingSystem.IsMacOS().
/// </summary>
internal static class ObjCRuntime
{
    private const string ObjCLib = "/usr/lib/libobjc.dylib";

    // --- Core runtime functions ---

    [DllImport(ObjCLib, EntryPoint = "objc_getClass")]
    internal static extern IntPtr objc_getClass(string className);

    [DllImport(ObjCLib, EntryPoint = "sel_registerName")]
    internal static extern IntPtr sel_registerName(string selectorName);

    [DllImport(ObjCLib, EntryPoint = "objc_allocateClassPair")]
    internal static extern IntPtr objc_allocateClassPair(IntPtr superclass, string name, int extraBytes);

    [DllImport(ObjCLib, EntryPoint = "objc_registerClassPair")]
    internal static extern void objc_registerClassPair(IntPtr cls);

    [DllImport(ObjCLib, EntryPoint = "class_addMethod")]
    internal static extern bool class_addMethod(IntPtr cls, IntPtr sel, IntPtr imp, string types);

    // --- objc_msgSend overloads ---
    // Each overload matches a different ObjC message signature.

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3);

    // For addObserver:selector:name:object: — 4 pointer-sized args.
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2, IntPtr arg3, IntPtr arg4);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, bool arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, long arg1);

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, ulong arg1);

    // For dataWithBytes:length: (IntPtr bytes, nuint length)
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, nuint arg2);

    // For printOperationForPrintInfo:scalingMode:autoRotate:
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, long arg2, bool arg3);

    // For methods returning BOOL (e.g. runOperation)
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern byte objc_msgSend_bool(IntPtr receiver, IntPtr selector);

    // For methods returning int (e.g. -[NSAppleEventDescriptor numberOfItems])
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern int objc_msgSend_int(IntPtr receiver, IntPtr selector);

    // For -[NSAppleEventDescriptor descriptorAtIndex:] (1-based)
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend_index(IntPtr receiver, IntPtr selector, long index);

    // For -[NSAppleEventDescriptor paramDescriptorForKeyword:] where keyword is a FourCharCode (uint)
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend_fourcc(IntPtr receiver, IntPtr selector, uint keyword);

    // For -[NSAppleEventManager setEventHandler:andSelector:forEventClass:andEventID:]
    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern void objc_msgSend_setEventHandler(
        IntPtr receiver, IntPtr selector,
        IntPtr handler, IntPtr selArg,
        uint eventClass, uint eventId);

    // For loading frameworks (e.g. Quartz.framework for PDFKit)
    [DllImport("libdl.dylib")]
    internal static extern IntPtr dlopen(string path, int mode);

    internal const int RTLD_LAZY = 1;

    // FourCharCode helpers (Apple Event manager uses packed big-endian ASCII).
    internal static uint FourCharCode(string code)
    {
        if (code == null || code.Length != 4)
            throw new ArgumentException("FourCharCode must be exactly 4 ASCII characters.", nameof(code));
        return ((uint)code[0] << 24) | ((uint)code[1] << 16) | ((uint)code[2] << 8) | (uint)code[3];
    }

    /// <summary>
    /// Loads Cocoa.framework (AppKit + Foundation) if not already loaded.
    /// NSApplication typically loads it at launch, but this is a safe explicit hook
    /// for code paths that depend on AppKit/Foundation symbols being resolved.
    /// </summary>
    private static IntPtr _cocoaHandle;
    internal static void LoadCocoa()
    {
        if (_cocoaHandle != IntPtr.Zero) return;
        _cocoaHandle = dlopen("/System/Library/Frameworks/Cocoa.framework/Cocoa", RTLD_LAZY);
    }

    // --- Delegate type for ObjC action methods (target-action pattern) ---

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ActionDelegate(IntPtr self, IntPtr selector, IntPtr sender);

    /// <summary>
    /// Delegate for ObjC methods that return an IntPtr (e.g., toolbar delegate methods).
    /// Signature: -(id)method:(id)arg1 arg2:(id)arg2 arg3:(BOOL)arg3
    /// </summary>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr ReturnDelegate(IntPtr self, IntPtr selector, IntPtr arg1);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr ReturnDelegate3(IntPtr self, IntPtr selector, IntPtr arg1, IntPtr arg2, byte arg3);

    // --- Helper methods ---

    /// <summary>
    /// Creates an NSString from a C# string.
    /// </summary>
    internal static IntPtr CreateNSString(string str)
    {
        IntPtr nsStringClass = objc_getClass("NSString");
        IntPtr alloc = objc_msgSend(nsStringClass, sel_registerName("alloc"));
        IntPtr utf8 = Marshal.StringToCoTaskMemUTF8(str);
        try
        {
            IntPtr nsString = objc_msgSend(alloc, sel_registerName("initWithUTF8String:"), utf8);
            return nsString;
        }
        finally
        {
            Marshal.FreeCoTaskMem(utf8);
        }
    }

    /// <summary>
    /// Creates a new NSMenu with the given title.
    /// </summary>
    internal static IntPtr CreateMenu(string title)
    {
        IntPtr nsMenuClass = objc_getClass("NSMenu");
        IntPtr alloc = objc_msgSend(nsMenuClass, sel_registerName("alloc"));
        IntPtr titleStr = CreateNSString(title);
        return objc_msgSend(alloc, sel_registerName("initWithTitle:"), titleStr);
    }

    /// <summary>
    /// Creates an NSMenuItem with title, action selector, and key equivalent.
    /// </summary>
    internal static IntPtr CreateMenuItem(string title, IntPtr action, string keyEquivalent)
    {
        IntPtr nsMenuItemClass = objc_getClass("NSMenuItem");
        IntPtr alloc = objc_msgSend(nsMenuItemClass, sel_registerName("alloc"));
        IntPtr titleStr = CreateNSString(title);
        IntPtr keyStr = CreateNSString(keyEquivalent);
        return objc_msgSend(alloc, sel_registerName("initWithTitle:action:keyEquivalent:"),
            titleStr, action, keyStr);
    }

    /// <summary>
    /// Creates a separator NSMenuItem.
    /// </summary>
    internal static IntPtr CreateSeparatorItem()
    {
        IntPtr nsMenuItemClass = objc_getClass("NSMenuItem");
        return objc_msgSend(nsMenuItemClass, sel_registerName("separatorItem"));
    }

    /// <summary>
    /// Adds an item to a menu.
    /// </summary>
    internal static void AddItemToMenu(IntPtr menu, IntPtr item)
    {
        objc_msgSend(menu, sel_registerName("addItem:"), item);
    }

    /// <summary>
    /// Sets the submenu of a menu item.
    /// </summary>
    internal static void SetSubmenu(IntPtr menuItem, IntPtr submenu)
    {
        objc_msgSend(menuItem, sel_registerName("setSubmenu:"), submenu);
    }

    /// <summary>
    /// Sets the target (receiver) of a menu item's action.
    /// </summary>
    internal static void SetTarget(IntPtr menuItem, IntPtr target)
    {
        objc_msgSend(menuItem, sel_registerName("setTarget:"), target);
    }

    /// <summary>
    /// Sets the key equivalent modifier mask on a menu item.
    /// NSEventModifierFlagCommand = 1 &lt;&lt; 20 = 0x100000
    /// NSEventModifierFlagShift   = 1 &lt;&lt; 17 = 0x020000
    /// NSEventModifierFlagOption  = 1 &lt;&lt; 19 = 0x080000
    /// </summary>
    internal static void SetKeyEquivalentModifierMask(IntPtr menuItem, ulong mask)
    {
        objc_msgSend(menuItem, sel_registerName("setKeyEquivalentModifierMask:"), mask);
    }

    /// <summary>
    /// Sets the hidden state of a menu item.
    /// </summary>
    internal static void SetHidden(IntPtr menuItem, bool hidden)
    {
        objc_msgSend(menuItem, sel_registerName("setHidden:"), hidden);
    }

    /// <summary>
    /// Sets the enabled state of a menu item.
    /// </summary>
    internal static void SetEnabled(IntPtr menuItem, bool enabled)
    {
        objc_msgSend(menuItem, sel_registerName("setEnabled:"), enabled);
    }

    /// <summary>
    /// Disables auto-enabling of menu items on a menu so we control enabled state manually.
    /// </summary>
    internal static void SetAutoenablesItems(IntPtr menu, bool autoEnable)
    {
        objc_msgSend(menu, sel_registerName("setAutoenablesItems:"), autoEnable);
    }

    /// <summary>
    /// Sets the view of an NSMenuItem (for embedding custom controls like NSSearchField).
    /// </summary>
    internal static void SetView(IntPtr menuItem, IntPtr view)
    {
        objc_msgSend(menuItem, sel_registerName("setView:"), view);
    }

    /// <summary>
    /// Gets the string value of an NSControl/NSTextField.
    /// </summary>
    internal static string GetStringValue(IntPtr control)
    {
        IntPtr nsString = objc_msgSend(control, sel_registerName("stringValue"));
        if (nsString == IntPtr.Zero) return string.Empty;
        IntPtr utf8Ptr = objc_msgSend(nsString, sel_registerName("UTF8String"));
        return utf8Ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(utf8Ptr) ?? string.Empty;
    }

    /// <summary>
    /// Converts an NSString pointer to a managed string via its UTF8 representation.
    /// </summary>
    internal static string NSStringToUtf8(IntPtr nsString)
    {
        if (nsString == IntPtr.Zero) return string.Empty;
        IntPtr utf8Ptr = objc_msgSend(nsString, sel_registerName("UTF8String"));
        return utf8Ptr == IntPtr.Zero ? string.Empty : Marshal.PtrToStringUTF8(utf8Ptr) ?? string.Empty;
    }

    /// <summary>
    /// Given an NSURL pointer (file URL), returns its file-system path as a managed string.
    /// Returns empty string if the URL is null or not a file URL.
    /// </summary>
    internal static string NSUrlToPath(IntPtr nsUrl)
    {
        if (nsUrl == IntPtr.Zero) return string.Empty;
        IntPtr pathStr = objc_msgSend(nsUrl, sel_registerName("path"));
        return NSStringToUtf8(pathStr);
    }

    /// <summary>
    /// Sets the string value of an NSControl/NSTextField.
    /// </summary>
    internal static void SetStringValue(IntPtr control, string value)
    {
        IntPtr nsStr = CreateNSString(value ?? string.Empty);
        objc_msgSend(control, sel_registerName("setStringValue:"), nsStr);
    }

    /// <summary>
    /// Sets the placeholder string of an NSSearchField/NSTextField.
    /// </summary>
    internal static void SetPlaceholderString(IntPtr textField, string placeholder)
    {
        IntPtr nsStr = CreateNSString(placeholder);
        objc_msgSend(textField, sel_registerName("setPlaceholderString:"), nsStr);
    }

    // --- CGRect for initWithFrame: ---

    [StructLayout(LayoutKind.Sequential)]
    internal struct CGRect
    {
        public double X, Y, Width, Height;
        public CGRect(double x, double y, double width, double height)
        {
            X = x; Y = y; Width = width; Height = height;
        }
    }

    [DllImport(ObjCLib, EntryPoint = "objc_msgSend")]
    internal static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, CGRect arg1);

    /// <summary>
    /// Creates an NSSearchField positioned at (x, y) with the given width and height.
    /// </summary>
    internal static IntPtr CreateSearchField(double x, double y, double width, double height)
    {
        IntPtr nsSearchFieldClass = objc_getClass("NSSearchField");
        IntPtr alloc = objc_msgSend(nsSearchFieldClass, sel_registerName("alloc"));
        CGRect frame = new(x, y, width, height);
        return objc_msgSend(alloc, sel_registerName("initWithFrame:"), frame);
    }

    /// <summary>
    /// Creates a bare NSView with the given frame. Useful as a padded container
    /// for other controls inside an NSMenuItem.
    /// </summary>
    internal static IntPtr CreateView(double width, double height)
    {
        IntPtr nsViewClass = objc_getClass("NSView");
        IntPtr alloc = objc_msgSend(nsViewClass, sel_registerName("alloc"));
        CGRect frame = new(0, 0, width, height);
        return objc_msgSend(alloc, sel_registerName("initWithFrame:"), frame);
    }

    /// <summary>
    /// Adds a child view as a subview of the parent.
    /// </summary>
    internal static void AddSubview(IntPtr parent, IntPtr child)
    {
        objc_msgSend(parent, sel_registerName("addSubview:"), child);
    }

    /// <summary>
    /// Sets the action selector on an NSControl (e.g. NSSearchField).
    /// </summary>
    internal static void SetAction(IntPtr control, IntPtr actionSelector)
    {
        objc_msgSend(control, sel_registerName("setAction:"), actionSelector);
    }

    /// <summary>
    /// Creates an NSArray containing the given NSString objects.
    /// </summary>
    internal static IntPtr CreateNSArray(params IntPtr[] objects)
    {
        IntPtr nsArrayClass = objc_getClass("NSMutableArray");
        IntPtr array = objc_msgSend(nsArrayClass, sel_registerName("array"));
        foreach (var obj in objects)
        {
            objc_msgSend(array, sel_registerName("addObject:"), obj);
        }
        return array;
    }

    // --- Modifier mask constants ---
    internal const ulong NSEventModifierFlagCommand = 1UL << 20;
    internal const ulong NSEventModifierFlagShift = 1UL << 17;
    internal const ulong NSEventModifierFlagOption = 1UL << 19;
}
