using System;
using System.Runtime.Serialization;

namespace EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Exceptions
{
    [Serializable]
    public class ZIndexMismatchException : Exception
    {
        /// <summary>
        /// An exception to be invoked if this class's constructor attempts to initialise
        /// a sorted list of TileMaps but there is a missing Z index.
        /// </summary>
        public ZIndexMismatchException(string message) : base(message) { }

        public ZIndexMismatchException()
        {
        }

        public ZIndexMismatchException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ZIndexMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}