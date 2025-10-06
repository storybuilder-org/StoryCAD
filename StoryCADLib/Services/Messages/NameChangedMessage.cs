using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StoryCADLib.Services.Messages;

public class NameChangedMessage : ValueChangedMessage<NameChangeMessage>
{
    public NameChangedMessage(NameChangeMessage value) : base(value)
    {
    }
}
