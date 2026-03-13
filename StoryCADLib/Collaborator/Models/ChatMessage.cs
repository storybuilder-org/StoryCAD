using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace StoryCADLib.Collaborator.Models;

/// <summary>
/// Represents a message in the workflow chat conversation.
/// </summary>
public class ChatMessage
{
    /// <summary>
    /// The message text content.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// True if this is a user message, false if from Collaborator.
    /// </summary>
    public bool IsUser { get; set; }

    /// <summary>
    /// Display label for the message sender.
    /// </summary>
    public string Sender => IsUser ? "You" : "Collaborator";

    /// <summary>
    /// Background brush for the message bubble.
    /// User messages: blue tint, Collaborator messages: gray tint.
    /// </summary>
    public SolidColorBrush BackgroundBrush => IsUser
        ? new SolidColorBrush(Color.FromArgb(255, 0, 120, 212))    // Blue for user
        : new SolidColorBrush(Color.FromArgb(255, 60, 60, 60));    // Dark gray for Collaborator

    /// <summary>
    /// Text color brush for the message.
    /// </summary>
    public SolidColorBrush TextBrush => new SolidColorBrush(Colors.White);

    /// <summary>
    /// Horizontal alignment for the message bubble.
    /// User messages align right, Collaborator messages align left.
    /// </summary>
    public Microsoft.UI.Xaml.HorizontalAlignment BubbleAlignment => IsUser
        ? Microsoft.UI.Xaml.HorizontalAlignment.Right
        : Microsoft.UI.Xaml.HorizontalAlignment.Left;

    /// <summary>
    /// Creates a user message.
    /// </summary>
    public static ChatMessage FromUser(string text) => new() { Text = text, IsUser = true };

    /// <summary>
    /// Creates a Collaborator response message.
    /// </summary>
    public static ChatMessage FromCollaborator(string text) => new() { Text = text, IsUser = false };

    /// <summary>
    /// Creates an error message (displayed as Collaborator).
    /// </summary>
    public static ChatMessage Error(string text) => new() { Text = $"Error: {text}", IsUser = false };
}
