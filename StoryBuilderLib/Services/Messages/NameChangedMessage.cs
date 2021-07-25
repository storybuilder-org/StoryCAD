using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace StoryBuilder.Services.Messages
{
    public class NameChangedMessage : ValueChangedMessage<NameChangeMessage>
    {
        public NameChangedMessage(NameChangeMessage value) : base(value)
        {
        }

    }
}
