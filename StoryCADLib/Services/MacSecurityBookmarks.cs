using System.Runtime.InteropServices;
using System.Text;
using StoryCADLib.Services.Logging;

namespace StoryCADLib.Services;

/// <summary>
/// Manages macOS security-scoped bookmarks to persist folder/file access across app launches.
/// Bookmark data is stored in PreferencesModel.SecurityBookmarks.
/// All methods are no-ops on non-macOS platforms.
///
/// Lifetime: startAccessingSecurityScopedResource is called at app startup for each restored
/// bookmark and kept open for the entire session. Apple docs require balanced stop calls, but
/// since these are app-wide grants needed for the full lifetime, we intentionally do not
/// call stopAccessingSecurityScopedResource — the OS reclaims them on process exit.
/// </summary>
public static class MacSecurityBookmarks
{
    private const int MaxBookmarks = 50;
    private const ulong NSURLBookmarkCreationWithSecurityScope = 1 << 11; // 2048
    private const ulong NSURLBookmarkResolutionWithSecurityScope = 1 << 10; // 1024

    #region P/Invoke declarations

    [DllImport("libobjc.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr objc_getClass(string className);

    [DllImport("libobjc.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string selector);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector, IntPtr arg1, IntPtr arg2);

    // bookmarkDataWithOptions:includingResourceValuesForKeys:relativeToURL:error:
    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_bookmark(
        IntPtr receiver, IntPtr selector,
        ulong options, IntPtr keys, IntPtr relativeURL, out IntPtr error);

    // initByResolvingBookmarkData:options:relativeToURL:bookmarkDataIsStale:error:
    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_resolve(
        IntPtr receiver, IntPtr selector,
        IntPtr bookmarkData, ulong options, IntPtr relativeURL,
        out byte isStale, out IntPtr error);

    // dataWithBytes:length: — copies the bytes into a new NSData; safe to free source after call
    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend_data(
        IntPtr receiver, IntPtr selector,
        IntPtr bytes, nuint length);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern byte objc_msgSend_bool(IntPtr receiver, IntPtr selector);

    [DllImport("libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern nuint objc_msgSend_nuint(IntPtr receiver, IntPtr selector);

    #endregion

    #region ObjC helpers

    private static readonly IntPtr ReleaseSel = sel_registerName("release");

    private static void ReleaseObjC(IntPtr obj)
    {
        if (obj != IntPtr.Zero)
            objc_msgSend(obj, ReleaseSel);
    }

    private static IntPtr CreateNSString(string str)
    {
        var nsStringClass = objc_getClass("NSString");
        var alloc = sel_registerName("alloc");
        var initWithUTF8 = sel_registerName("initWithUTF8String:");

        var allocated = objc_msgSend(nsStringClass, alloc);
        var utf8Bytes = Encoding.UTF8.GetBytes(str + "\0");
        var pinned = GCHandle.Alloc(utf8Bytes, GCHandleType.Pinned);
        try
        {
            return objc_msgSend(allocated, initWithUTF8, pinned.AddrOfPinnedObject());
        }
        finally
        {
            pinned.Free();
        }
    }

    private static string NSStringToString(IntPtr nsString)
    {
        if (nsString == IntPtr.Zero) return null;
        var utf8 = sel_registerName("UTF8String");
        var ptr = objc_msgSend(nsString, utf8);
        return ptr == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(ptr);
    }

    private static IntPtr CreateNSURL(string path)
    {
        var nsUrlClass = objc_getClass("NSURL");
        var sel = sel_registerName("fileURLWithPath:");
        var nsPath = CreateNSString(path);
        // fileURLWithPath: returns an autoreleased object — do NOT release it.
        // The NSString from CreateNSString is alloc/init'd (+1) so we release it.
        var nsUrl = objc_msgSend(nsUrlClass, sel, nsPath);
        ReleaseObjC(nsPath);
        return nsUrl;
    }

    private static string GetNSURLPath(IntPtr nsUrl)
    {
        if (nsUrl == IntPtr.Zero) return null;
        var pathSel = sel_registerName("path");
        var nsPath = objc_msgSend(nsUrl, pathSel);
        return NSStringToString(nsPath);
    }

    #endregion

    /// <summary>
    /// Creates a security-scoped bookmark for the given path and stores it in the
    /// provided dictionary. The caller is responsible for persisting preferences.
    /// Oldest entries are pruned when the dictionary exceeds <see cref="MaxBookmarks"/>.
    /// No-op on non-macOS platforms.
    /// </summary>
    public static void SaveBookmark(string path, Dictionary<string, string> bookmarks, ILogService log)
    {
        if (!OperatingSystem.IsMacOS()) return;
        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            // CreateNSURL returns an autoreleased NSURL — do not release
            var nsUrl = CreateNSURL(path);
            if (nsUrl == IntPtr.Zero)
            {
                log.Log(LogLevel.Error, $"Bookmark: Failed to create NSURL for {path}");
                return;
            }

            var sel = sel_registerName("bookmarkDataWithOptions:includingResourceValuesForKeys:relativeToURL:error:");
            // bookmarkDataWithOptions: returns autoreleased NSData — do not release
            var bookmarkData = objc_msgSend_bookmark(
                nsUrl, sel,
                NSURLBookmarkCreationWithSecurityScope,
                IntPtr.Zero, IntPtr.Zero, out var error);

            if (bookmarkData == IntPtr.Zero || error != IntPtr.Zero)
            {
                log.Log(LogLevel.Error, $"Bookmark: Failed to create bookmark data for {path}");
                return;
            }

            var lengthSel = sel_registerName("length");
            var bytesSel = sel_registerName("bytes");
            var length = (int)objc_msgSend_nuint(bookmarkData, lengthSel);
            var bytesPtr = objc_msgSend(bookmarkData, bytesSel);

            if (length <= 0 || bytesPtr == IntPtr.Zero)
            {
                log.Log(LogLevel.Error, $"Bookmark: Empty bookmark data for {path}");
                return;
            }

            var bytes = new byte[length];
            Marshal.Copy(bytesPtr, bytes, 0, length);

            // Upsert into the dictionary
            bookmarks[path] = Convert.ToBase64String(bytes);

            // Prune oldest entries if over the cap
            while (bookmarks.Count > MaxBookmarks)
            {
                var oldest = bookmarks.Keys.First();
                bookmarks.Remove(oldest);
            }

            log.Log(LogLevel.Info, $"Bookmark: Saved bookmark for {path} ({length} bytes, {bookmarks.Count} total)");
        }
        catch (Exception ex)
        {
            log.Log(LogLevel.Error, $"Bookmark: Exception saving bookmark: {ex.Message}");
        }
    }

    /// <summary>
    /// Resolves a single bookmark and starts accessing the security-scoped resource.
    /// Returns the resolved path, or null on failure.
    /// </summary>
    private static string RestoreEntry(string path, string base64Data,
        Dictionary<string, string> bookmarks, ILogService log)
    {
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64Data);
        }
        catch
        {
            log.Log(LogLevel.Warn, $"Bookmark: Invalid base64 data for {path}");
            return null;
        }

        if (bytes.Length == 0) return null;

        // dataWithBytes:length: returns autoreleased NSData — do not release.
        // It copies the bytes, so the pinned source can be freed immediately.
        var nsDataClass = objc_getClass("NSData");
        var dataWithBytesSel = sel_registerName("dataWithBytes:length:");
        var pinnedBytes = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        IntPtr nsData;
        try
        {
            nsData = objc_msgSend_data(nsDataClass, dataWithBytesSel,
                pinnedBytes.AddrOfPinnedObject(), (nuint)bytes.Length);
        }
        finally
        {
            pinnedBytes.Free();
        }

        if (nsData == IntPtr.Zero)
        {
            log.Log(LogLevel.Error, $"Bookmark: Failed to create NSData for {path}");
            return null;
        }

        // alloc/initByResolvingBookmarkData: returns +1 retained NSURL.
        // We keep it alive for startAccessingSecurityScopedResource (never released — see class doc).
        var nsUrlClass = objc_getClass("NSURL");
        var allocSel = sel_registerName("alloc");
        var resolveSel = sel_registerName("initByResolvingBookmarkData:options:relativeToURL:bookmarkDataIsStale:error:");

        var allocated = objc_msgSend(nsUrlClass, allocSel);
        var nsUrl = objc_msgSend_resolve(
            allocated, resolveSel,
            nsData,
            NSURLBookmarkResolutionWithSecurityScope,
            IntPtr.Zero,
            out var isStale,
            out var error);

        if (nsUrl == IntPtr.Zero)
        {
            log.Log(LogLevel.Warn, $"Bookmark: Failed to resolve bookmark for {path}");
            return null;
        }

        // Start accessing the security-scoped resource.
        // We intentionally do NOT call stopAccessingSecurityScopedResource — see class doc.
        var startSel = sel_registerName("startAccessingSecurityScopedResource");
        var started = objc_msgSend_bool(nsUrl, startSel) != 0;

        if (!started)
        {
            log.Log(LogLevel.Warn, $"Bookmark: startAccessingSecurityScopedResource failed for {path}");
            return null;
        }

        var resolvedPath = GetNSURLPath(nsUrl);

        if (isStale != 0)
        {
            log.Log(LogLevel.Warn, $"Bookmark: {path} was stale, re-saving");
            SaveBookmark(resolvedPath, bookmarks, log);
        }

        return resolvedPath;
    }

    /// <summary>
    /// Restores all saved bookmarks from the preferences dictionary. Called once at startup.
    /// No-op on non-macOS platforms.
    /// </summary>
    public static void RestoreAllBookmarks(Dictionary<string, string> bookmarks, ILogService log)
    {
        if (!OperatingSystem.IsMacOS()) return;

        if (bookmarks.Count == 0)
        {
            log.Log(LogLevel.Info, "Bookmark: No saved bookmarks to restore");
            return;
        }

        log.Log(LogLevel.Info, $"Bookmark: Restoring {bookmarks.Count} security-scoped bookmarks");
        var restored = 0;
        foreach (var (path, data) in bookmarks.ToList())
        {
            try
            {
                var resolved = RestoreEntry(path, data, bookmarks, log);
                if (resolved != null) restored++;
            }
            catch (Exception ex)
            {
                log.Log(LogLevel.Error, $"Bookmark: Exception restoring {path}: {ex.Message}");
            }
        }

        log.Log(LogLevel.Info, $"Bookmark: Restored {restored}/{bookmarks.Count} bookmarks");
    }
}
