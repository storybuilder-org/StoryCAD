using System;
using System.Threading.Tasks;
using Microsoft.UI.Dispatching;

namespace StoryCAD.Services.Locking
{
    public static class DispatcherQueueExtensions
    {
        public static Task EnqueueAsync(this DispatcherQueue dq, Action action)
        {
            // Fallbacks so unit tests (no UI thread) don't hang
            if (dq is null)
            {
                action();
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<object?>();
            // If TryEnqueue fails, run inline to avoid deadlock in headless tests
            if (!dq.TryEnqueue(() =>
            {
                try { action(); tcs.SetResult(null); }
                catch (Exception ex) { tcs.SetException(ex); }
            }))
            {
                try { action(); tcs.SetResult(null); }
                catch (Exception ex) { tcs.SetException(ex); }
            }
            return tcs.Task;
        }

        public static Task EnqueueAsync(this DispatcherQueue dq, Func<Task> func)
        {
            if (dq is null)
            {
                return func();
            }

            var tcs = new TaskCompletionSource<object?>();
            if (!dq.TryEnqueue(async () =>
            {
                try { await func(); tcs.SetResult(null); }
                catch (Exception ex) { tcs.SetException(ex); }
            }))
            {
                // Fallback if enqueue failed
                return func();
            }
            return tcs.Task;
        }
    }
}