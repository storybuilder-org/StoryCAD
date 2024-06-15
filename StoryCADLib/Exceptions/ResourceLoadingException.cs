using System;

namespace StoryCAD.Exceptions
{
    public class ResourceLoadingException : Exception
    {
        /// <summary>
        /// This custom exception replaces MissingManifestResourceException and is
        /// used when catching errors loading data such as lists and report templates
        /// from the Assets folder's manifest resources.
        ///
        /// MissingManifestResourceException is deprecated and results in the compile-time
        /// warning SYSLIB0051: 'Exception.Exception(SerializationInfo, StreamingContext)' is obsolete:
        /// 'This API supports obsolete formatter-based serialization. It should not be called or
        /// extended by application code.' (https://aka.ms/dotnet-warnings/SYSLIB0051)
        ///  
        /// </summary>
        public ResourceLoadingException()
            : base("An error has occurred, please reinstall or update StoryCAD to continue.")
        {
        }
    }
}
