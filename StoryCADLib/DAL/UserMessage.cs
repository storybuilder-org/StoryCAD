#nullable enable

namespace StoryCADLib.DAL;

public sealed record UserMessage(
    int MessageId,
    string Subject,
    string Body,
    string? LinkUrl,
    string? LinkText,
    DateTime CreatedAt);
