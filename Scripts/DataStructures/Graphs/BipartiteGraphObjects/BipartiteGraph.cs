using System.Collections.Generic;
using System.Linq;

namespace EFSMono.Scripts.DataStructures.Graphs.BipartiteGraphObjects
{
/// <summary>
/// Bipartite Graph class.
/// Separates nodes into two sides and contains functions such as Ford-Fulkerson to get the Max Matching, etc.
/// </summary>
public class BipartiteGraph : GenericGraph<BipartiteGraphNode>
{
    public HashSet<int> leftNodeIDs { get; }
    public HashSet<int> rightNodeIDs { get; }

    public BipartiteGraph(IReadOnlyCollection<BipartiteGraphNode> nodeCollection) : base(nodeCollection)
    {
        this.leftNodeIDs = new HashSet<int>();
        this.rightNodeIDs = new HashSet<int>();
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

    /// <summary>
    /// Obtain the MVC by:
    ///     1. Obtaining the Max Matching (MM)
    ///     2. Taking left-side nodes excluded from the MM
    ///     3. Getting the DFS cover of those left-side excluded nodes
    ///     4. Taking right-side visited nodes and left-side UNVISITED nodes in the DFS cover
    /// Voila, MVC.
    /// </summary>
    /// <returns>The Max Vertex Cover, in a hashset containing BipartiteNodes.</returns>
    public HashSet<BipartiteGraphNode> GetMaxVertexCover()
    {
        Dictionary<BipartiteGraphNode, BipartiteGraphNode> maxMatching = this.GetMaxMatching();
        List<BipartiteGraphNode> excludedLeftNodes = leftNodeIDs.Select(leftNodeID => this.nodes[leftNodeID])
            .Where(leftNode => !maxMatching.ContainsKey(leftNode)).ToList();
        List<BipartiteGraphNode> dfsCover = this.GetDFSCover(excludedLeftNodes);
        
        //Find Max Vertex Cover (right side visited, left side unvisited of DFS cover)
        var maxVertexCover = new HashSet<BipartiteGraphNode>();
        foreach (int rightNodeID in this.rightNodeIDs.Where(rightNodeID => dfsCover.Contains(this.nodes[rightNodeID])))
        { //add right-side nodes included in DFS cover to MVC
            maxVertexCover.Add(this.nodes[rightNodeID]);
        }

        foreach (int leftNodeID in this.leftNodeIDs.Where(leftNodeID => !dfsCover.Contains(this.nodes[leftNodeID])))
        { //add left-side nodes NOT included in DFS cover to MVC
            maxVertexCover.Add(this.nodes[leftNodeID]);
        }
        return maxVertexCover;
    }

    /// <summary>
    /// Returns the Max Independent Set, which is the conjugate of the Max Vertex Cover.
    /// </summary>
    /// <returns>A HashSet consisting of the MIS.</returns>
    public HashSet<BipartiteGraphNode> GetMaxIndependentSet()
    {
        HashSet<BipartiteGraphNode> maxVertexCover = this.GetMaxVertexCover();
        var maxIndependentSet = new HashSet<BipartiteGraphNode>(this.nodes.Values);
        maxIndependentSet.ExceptWith((maxVertexCover));
        return maxIndependentSet;
    }
}
}