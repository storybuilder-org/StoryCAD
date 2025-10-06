using CommunityToolkit.Mvvm.Messaging.Messages;

namespace StoryCAD.Services.Messages;

public class IsBackupStatusMessage : ValueChangedMessage<bool>
{
    public IsBackupStatusMessage(bool value) : base(value)
    {
    }
}
