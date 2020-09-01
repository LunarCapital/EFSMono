using SCol = System.Collections.Generic;

namespace EFSMono.Common.DataStructures.Graphs.BipartiteGraphObjects
{
    /// <summary>
    /// Bipartite Graph Node.
    /// Stores bipartite side (left/right) and connected nodes.
    /// </summary>
    public class BipartiteGraphNode : GenericGraphNode
    {
        public enum BipartiteSide { Left = 0, Right = 1 };

        public BipartiteSide side { get; private set; }

        public BipartiteGraphNode(int id, SCol.List<int> connectedNodeIDs, BipartiteSide side) : base(id, connectedNodeIDs)
        {
            this.side = side;
        }
    }
}