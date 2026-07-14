#if HAS_UNO
using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StoryCADLib.Services.Store;

namespace StoryCADTests.Services.Store;

/// <summary>
///     Verifies the interop's DllImport resolver actually loads the shim dylib on macOS. Only a
///     dlopen, no live StoreKit call, so it needs no App Store account. Self-skips off macOS or when
///     the dylib has not been built (run src/macos/StoreKitShim/build.sh). Not a substitute for the
///     sandbox manual checklist in the IAP testing doc
///     (StoryCADWiki: wiki/repos/Collaborator/sources/iap-billing-docs.md).
/// </summary>
[TestClass]
public class StoreKitInteropSmokeTests
{
    [TestMethod]
    public void IsAvailable_ResolvesDylibWhenPresent()
    {
        if (!OperatingSystem.IsMacOS())
        {
            Assert.Inconclusive("StoreKit shim is macOS-only.");
            return;
        }

        var beside = Path.Combine(AppContext.BaseDirectory, "libStoryCADStoreKit.dylib");
        if (!File.Exists(beside))
        {
            var built = FindBuiltDylib();
            if (built is null)
            {
                Assert.Inconclusive("Shim dylib not built; run src/macos/StoreKitShim/build.sh.");
                return;
            }

            File.Copy(built, beside, true);
        }

        Assert.IsTrue(StoreKitInterop.IsAvailable(),
            "the resolver should dlopen the dylib beside the test binary");
    }

    // Walk up from the test assembly to the repo root and look for build.sh's output.
    private static string FindBuiltDylib()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src", "macos", "StoreKitShim", "out",
                "libStoryCADStoreKit.dylib");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return null;
    }
}
#endif
