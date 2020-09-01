using System;
using System.Runtime.Serialization;

namespace EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Exceptions
{
    [Serializable]
    public class VerticesArraySizeMismatchException : Exception
    {
        /// <summary>
        /// An exception to be invoked if this class is constructed with an edges array that
        /// does not have a size of 4.
        /// </summary>
        public VerticesArraySizeMismatchException(string message) : base(message) { }

        public VerticesArraySizeMismatchException()
        {
        }

        public VerticesArraySizeMismatchException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected VerticesArraySizeMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}