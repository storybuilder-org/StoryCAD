using Microsoft.Toolkit.Mvvm.Messaging.Messages;
using System;

namespace StoryBuilder.Services.Messages
{
    public class NameChangedMessage : ValueChangedMessage<NameChangeMessage>
    {
        public NameChangedMessage(NameChangeMessage value) : base(value)
        {
        }

    }
}
