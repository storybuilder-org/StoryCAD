namespace StoryCollaborator.Models;

/// <summary>
/// The two messages the Worker sent to the model, returned on request as the first
/// <c>collab_prompt</c> SSE event of a workflow stream. Dev Workers only; a production
/// Worker never emits one. PromptTestRunner shows both so a prompt author can read what
/// the model was given.
/// </summary>
/// <param name="System">The coach system message, rendered for this call.</param>
/// <param name="User">The workflow template with the writer's values merged in.</param>
/// <param name="TemplateHash">SHA-256 hex of the template before merge, as in X-Template-Hash.</param>
/// <param name="SystemPromptHash">SHA-256 hex of the system message, as in X-System-Prompt-Hash.</param>
public sealed record ProxyPromptEcho(string System, string User, string? TemplateHash, string? SystemPromptHash);
