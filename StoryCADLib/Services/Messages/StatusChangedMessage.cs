using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StoryCADLib.Services.Messages;

public class StatusChangedMessage : ValueChangedMessage<StatusMessage>
{
    public StatusChangedMessage(StatusMessage value) : base(value)
    {
    }
}
