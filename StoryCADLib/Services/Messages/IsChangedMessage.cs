using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StoryCAD.Services.Messages;

public class IsChangedMessage : ValueChangedMessage<bool>
{
    public IsChangedMessage(bool value) : base(value)
    {
    }
}