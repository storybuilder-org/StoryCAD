using System.ComponentModel;
using System.Diagnostics;

namespace StoryCADAutomation.Driver;

/// <summary>
///     Win32 job object with JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE (plus die-on-unhandled-exception,
///     which suppresses the WER crash dialog for processes in the job). The launched StoryCAD is
///     assigned to the job, so even if the runner dies without running teardown, the OS closes
///     this handle and kills the app; a crashed child and its Windows Error Reporting dialog
///     cannot outlive the run (devdocs/issue_1421_dsl_design.md "Runner", Teardown paragraph).
/// </summary>
internal sealed class KillOnCloseJob : IDisposable
{
    private IntPtr _handle;

    /// <param name="allowCrashReporting">
    ///     Leave Windows Error Reporting on, so a crash writes a dump (CI, where WER's dialog is
    ///     turned off). Local runs keep it off so no crash dialog outlives the run.
    /// </param>
    public KillOnCloseJob(bool allowCrashReporting = false)
    {
        _handle = NativeMethods.CreateJobObjectW(IntPtr.Zero, null);
        if (_handle == IntPtr.Zero)
        {
            throw new Win32Exception();
        }

        if (!NativeMethods.SetKillOnCloseLimits(_handle, allowCrashReporting))
        {
            var error = new Win32Exception();
            NativeMethods.CloseHandle(_handle);
            _handle = IntPtr.Zero;
            throw error;
        }
    }

    /// <summary>
    ///     Puts a process into the job. Assignment happens right after Process.Start; the
    ///     brief unassigned window is the standard trade-off for not launching suspended.
    /// </summary>
    public void Assign(Process process)
    {
        if (!NativeMethods.AssignProcessToJobObject(_handle, process.Handle))
        {
            throw new Win32Exception();
        }
    }

    /// <summary>Kills every process in the job immediately. Tolerates an already-empty job.</summary>
    public void TerminateAll()
    {
        if (_handle != IntPtr.Zero)
        {
            NativeMethods.TerminateJobObject(_handle, 1);
        }
    }

    /// <summary>Closing the handle is itself the kill switch (kill-on-close limit).</summary>
    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            NativeMethods.CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }
    }
}
