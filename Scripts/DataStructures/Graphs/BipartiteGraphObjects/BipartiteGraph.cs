using SCol = System.Collections.Generic;

namespace EFSMono.Scripts.DataStructures.Graphs.BipartiteGraphObjects
{
/// <summary>
/// Bipartite Graph class.
/// Separates nodes into two sides and contains functions such as Ford-Fulkerson to get the Max Matching, etc.
/// </summary>
public class BipartiteGraph : GenericGraph<BipartiteGraphNode>
{
    public SCol.HashSet<int> leftNodeIDs { get; }
    public SCol.HashSet<int> rightNodeIDs { get; }

    public BipartiteGraph(SCol.IReadOnlyCollection<BipartiteGraphNode> nodeCollection) : base(nodeCollection)
    {
        this.leftNodeIDs = new SCol.HashSet<int>();
        this.rightNodeIDs = new SCol.HashSet<int>();
        foreach (BipartiteGraphNode node in nodeCollection)
        {
            if (node.side == BipartiteGraphNode.BipartiteSide.LEFT)
            {
                this.leftNodeIDs.Add(node.id);
            }
            else
            {
                this.rightNodeIDs.Add(node.id);
            }
        }
    }
}
}