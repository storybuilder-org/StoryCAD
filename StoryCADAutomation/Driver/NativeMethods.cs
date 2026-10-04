using System.Runtime.InteropServices;

namespace StoryCADAutomation.Driver;

/// <summary>
///     Win32 interop the driver needs beyond what FlaUI wraps: kill-on-close job objects
///     (a crashed child and its WER dialog must not outlive the run), foreground-window
///     identity (real input lands on whatever owns the pixels, so every real-pointer or
///     keyboard action verifies StoryCAD is foreground first), primary-display metrics
///     (the CI hosting section's resolution/DPI assertion at launch).
/// </summary>
internal static class NativeMethods
{
    // JOBOBJECT_BASIC_LIMIT_INFORMATION.LimitFlags values (winnt.h).
    internal const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

    // Processes in the job die on unhandled exceptions without the WER dialog; combined
    // with kill-on-close this is what keeps an unattended run from hanging on a crash UI.
    internal const uint JOB_OBJECT_LIMIT_DIE_ON_UNHANDLED_EXCEPTION = 0x0400;

    private const int JobObjectExtendedLimitInformation = 9;

    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;

    // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2. The driver process must be DPI-aware or
    // GetSystemMetrics returns virtualized values and pointer math goes wrong on scaled
    // displays; a plain console app is DPI-unaware by default.
    private static readonly IntPtr DpiAwarenessContextPerMonitorAwareV2 = new(-4);

    private static bool _dpiAwarenessApplied;

    /// <summary>
    ///     Makes this process per-monitor-v2 DPI aware, once. Failure is ignored: the call
    ///     fails with E_ACCESSDENIED when awareness was already set (e.g. by a manifest),
    ///     which is the state we want anyway.
    /// </summary>
    internal static void EnsureDpiAwareness()
    {
        if (_dpiAwarenessApplied)
        {
            return;
        }

        SetProcessDpiAwarenessContext(DpiAwarenessContextPerMonitorAwareV2);
        _dpiAwarenessApplied = true;
    }

    /// <summary>Primary desktop width in physical pixels.</summary>
    internal static int PrimaryScreenWidth() => GetSystemMetrics(SM_CXSCREEN);

    /// <summary>Primary desktop height in physical pixels.</summary>
    internal static int PrimaryScreenHeight() => GetSystemMetrics(SM_CYSCREEN);

    /// <summary>System DPI (96 = 100% scale).</summary>
    internal static int SystemDpi() => (int)GetDpiForSystem();

    /// <summary>Process id owning the current foreground window, or 0 when there is none.</summary>
    internal static uint ForegroundWindowProcessId()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return 0;
        }

        GetWindowThreadProcessId(hwnd, out var pid);
        return pid;
    }

    // --- job object ------------------------------------------------------------------

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern IntPtr CreateJobObjectW(IntPtr lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(
        IntPtr hJob, int jobObjectInformationClass, ref JOBOBJECT_EXTENDED_LIMIT_INFORMATION lpJobObjectInformation,
        uint cbJobObjectInformationLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool AssignProcessToJobObject(IntPtr hJob, IntPtr hProcess);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool TerminateJobObject(IntPtr hJob, uint uExitCode);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(IntPtr hObject);

    /// <summary>Applies the kill-on-close and die-on-unhandled-exception limits to a job.</summary>
    internal static bool SetKillOnCloseLimits(IntPtr jobHandle)
    {
        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION
        {
            BasicLimitInformation = new JOBOBJECT_BASIC_LIMIT_INFORMATION
            {
                LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE | JOB_OBJECT_LIMIT_DIE_ON_UNHANDLED_EXCEPTION,
            },
        };
        return SetInformationJobObject(
            jobHandle, JobObjectExtendedLimitInformation, ref info,
            (uint)Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>());
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION
    {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public UIntPtr MinimumWorkingSetSize;
        public UIntPtr MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public UIntPtr Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION
    {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public UIntPtr ProcessMemoryLimit;
        public UIntPtr JobMemoryLimit;
        public UIntPtr PeakProcessMemoryUsed;
        public UIntPtr PeakJobMemoryUsed;
    }

    // --- foreground / display ----------------------------------------------------------

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
}
