using StoryCADLib.DAL;

namespace StoryCADLib.Services.Dialogs;

public sealed partial class AdminMessagePage : Page
{
    public AdminMessagePage(UserMessage message)
    {
        InitializeComponent();
        BodyText.Text = message.Body;

        if (!string.IsNullOrWhiteSpace(message.LinkUrl) &&
            !string.IsNullOrWhiteSpace(message.LinkText) &&
            Uri.TryCreate(message.LinkUrl, UriKind.Absolute, out var uri))
        {
            MessageLink.NavigateUri = uri;
            MessageLink.Content = message.LinkText;
            MessageLink.Visibility = Visibility.Visible;
        }
    }
}
