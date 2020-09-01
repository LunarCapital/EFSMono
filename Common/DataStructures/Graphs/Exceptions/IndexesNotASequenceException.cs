using System;
using System.Runtime.Serialization;

namespace EFSMono.Common.DataStructures.Graphs
{
    [Serializable]
    public class IndexesNotASequenceException : Exception
    {
        /// <summary>
        /// An exception to be invoked if this class's constructor attempts to initialise
        /// a graph but its nodes are invalid, AKA IDs of its nodes do not form a sequence
        /// OR it has more than one node with the same ID (in which case one node will 'overwrite'
        /// another due to having the same Key in SortedList, and the # of nodes in this graph will
        /// be less than the number of nodes passed into its constructor)
        /// </summary>
        public IndexesNotASequenceException(string message) : base(message) { }

        public IndexesNotASequenceException()
        {
        }

        public IndexesNotASequenceException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IndexesNotASequenceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}