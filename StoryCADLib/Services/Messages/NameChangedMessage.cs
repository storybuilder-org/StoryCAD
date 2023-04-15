using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StoryCAD.Services.Messages;

public class NameChangedMessage : ValueChangedMessage<NameChangeMessage>
{
    public NameChangedMessage(NameChangeMessage value) : base(value)
    {
    }

}