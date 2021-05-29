using Microsoft.Toolkit.Mvvm.Messaging.Messages;

namespace StoryBuilder.Services.Messages
{
    public class IsDirtyChangedMessage : ValueChangedMessage<bool> 
    {
        public IsDirtyChangedMessage(bool value) : base(value)
        {
        }
    }
}
