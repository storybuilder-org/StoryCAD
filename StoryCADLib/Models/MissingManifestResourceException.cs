using System;
using System.Runtime.Serialization;

namespace StoryCAD.Models
{
    [Serializable]
    internal class MissingManifestResourceException : Exception
    {
        public MissingManifestResourceException()
        {
        }

        public MissingManifestResourceException(string message) : base(message)
        {
        }

        public MissingManifestResourceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MissingManifestResourceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}