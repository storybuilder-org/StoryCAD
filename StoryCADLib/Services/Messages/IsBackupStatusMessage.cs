using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StoryCADLib.Services.Messages;

public class IsBackupStatusMessage : ValueChangedMessage<bool>
{
    public IsBackupStatusMessage(bool value) : base(value)
    {
    }
}
