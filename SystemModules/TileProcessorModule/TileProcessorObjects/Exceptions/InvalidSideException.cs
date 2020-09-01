using System;
using System.Runtime.Serialization;

namespace EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Exceptions
{
    [Serializable]
    public class InvalidSideException : Exception
    {
        /// <summary>
        /// An exception to be invoked if this class's constructor attempts to initialise
        /// an edge with a side that does match up with the Globals side enum (NESW)
        /// </summary>
        public InvalidSideException(string message) : base(message) { }

        public InvalidSideException()
        {
        }

        public InvalidSideException(string message, Exception innerException) : base(message, innerException)
        {
        }
        protected InvalidSideException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}