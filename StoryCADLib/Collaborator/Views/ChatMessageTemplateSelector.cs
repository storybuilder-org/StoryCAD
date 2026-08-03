using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StoryCADLib.Collaborator.Models;

namespace StoryCADLib.Collaborator.Views;

/// <summary>
/// Picks the chat bubble or the rolled-up status template for a conversation row
/// (#129 chat cleanup).
/// </summary>
[Microsoft.UI.Xaml.Data.Bindable]
public sealed partial class ChatMessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate Bubble { get; set; }

    public DataTemplate Status { get; set; }

    protected override DataTemplate SelectTemplateCore(object item) =>
        item is ChatMessage { IsStatusGroup: true } ? Status : Bubble;

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container) =>
        SelectTemplateCore(item);
}
