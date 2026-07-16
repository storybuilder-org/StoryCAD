namespace StoryCADLib.Services.Store;

/// <summary>
///     Thrown when the Worker refuses an LLM call because the caller's balance is at or below
///     zero (issue #90 design section 10 "The cutoff": a 429 before any upstream dispatch).
///     Both Collaborator callers (<c>WorkflowRunner.PostToProxyAsync</c>,
///     <c>Collaborator</c>'s chat callback) recognize the Worker's 429 and throw this instead of
///     letting the generic <c>HttpRequestException</c> / <c>HttpOperationException</c> propagate,
///     so the message the user sees names the credits screen instead of reading
///     "Response status code does not indicate success: 429". Deliberately a distinct exception
///     type, not <see cref="System.Net.Http.HttpRequestException" />: WorkflowRunner's existing
///     <c>catch (HttpRequestException ex)</c> clause retries direct against OpenAI when
///     <c>OPENAI_API_KEY</c> is set (the fallback issue #90 step 8 retires) — an out-of-credits
///     refusal must never be caught there, or a capped user with a personal API key would route
///     straight around the balance cutoff.
/// </summary>
public sealed class OutOfCreditsException : Exception
{
    public OutOfCreditsException() : base(StoreConfig.OutOfCreditsMessage)
    {
    }
}
