using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace StoryBuilder.Services.Messages
{
    public class IsChangedMessage : ValueChangedMessage<bool>
    {
        public IsChangedMessage(bool value) : base(value)
        {
        }
    }
}
