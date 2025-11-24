using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StoryCADLib.Services.Messages;

public class IsAutosaveStatusMessage : ValueChangedMessage<bool>
{
    public IsAutosaveStatusMessage(bool value) : base(value)
    {
    }
}