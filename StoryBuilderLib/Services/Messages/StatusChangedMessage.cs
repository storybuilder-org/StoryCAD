using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StoryBuilder.Services.Messages;

public class StatusChangedMessage : ValueChangedMessage<StatusMessage>
{
    public StatusChangedMessage(StatusMessage value) : base(value)
    {
    }
}