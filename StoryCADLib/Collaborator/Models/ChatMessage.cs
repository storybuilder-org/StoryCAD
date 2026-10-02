using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace StoryCADLib.Collaborator.Models;

/// <summary>
/// Represents a message in the workflow chat conversation.
/// A message is either a chat bubble (user or Collaborator) or a rolled-up
/// status group — consecutive workflow progress lines shown as one collapsed
/// expander row instead of a bubble each (#129 chat cleanup).
/// </summary>
public partial class ChatMessage : ObservableObject
{
    /// <summary>
    /// The message text content (bubbles only; status groups use <see cref="Steps"/>).
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// True if this is a user message, false if from Collaborator.
    /// </summary>
    public bool IsUser { get; set; }

    /// <summary>
    /// True for a rolled-up run of workflow status lines.
    /// </summary>
    // get/set (not init): WinUI XamlTypeInfo assigns after construction on net10.0-windows
    public bool IsStatusGroup { get; set; }

    /// <summary>
    /// Status lines in this group (status groups only).
    /// </summary>
    public ObservableCollection<string> Steps { get; } = new();

    /// <summary>
    /// Collapsed header for a status group: latest step plus count.
    /// </summary>
    public string StatusHeader => Steps.Count switch
    {
        0 => string.Empty,
        1 => Steps[0],
        _ => $"{Steps[^1]}  ·  {Steps.Count} steps"
    };

    /// <summary>
    /// Appends a status line and refreshes the collapsed header.
    /// </summary>
    public void AddStep(string step)
    {
        Steps.Add(step);
        OnPropertyChanged(nameof(StatusHeader));
    }

    /// <summary>
    /// Display label for the message sender.
    /// </summary>
    public string Sender => IsUser ? "You" : "Collaborator";

    private bool _showSender = true;
    /// <summary>
    /// False when the previous message came from the same sender — the label is
    /// shown once per run of consecutive messages. Set by WorkflowViewModel.
    /// </summary>
    public bool ShowSender
    {
        get => _showSender;
        set
        {
            if (SetProperty(ref _showSender, value))
                OnPropertyChanged(nameof(SenderVisibility));
        }
    }

    /// <summary>
    /// Sender label visibility (classic Binding needs Visibility, not bool).
    /// </summary>
    public Microsoft.UI.Xaml.Visibility SenderVisibility => ShowSender
        ? Microsoft.UI.Xaml.Visibility.Visible
        : Microsoft.UI.Xaml.Visibility.Collapsed;

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

    /// <summary>
    /// Creates an empty status group; append lines with <see cref="AddStep"/>.
    /// </summary>
    public static ChatMessage StatusGroup() => new() { IsStatusGroup = true, IsUser = false };
}
