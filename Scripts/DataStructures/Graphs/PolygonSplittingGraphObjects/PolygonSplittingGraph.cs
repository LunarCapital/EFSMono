using System;
using System.Linq;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using Godot;
using SCol = System.Collections.Generic;

namespace EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects
{
/// <summary>
/// A graph specifically designed to split a polygon, which can be useful for two purpose:
///     1. Splitting a polygon-with-chords into chordless polygons
///     2. Splitting chordless polygons into rectangles
/// This graph's nodes represent polygon vertices, and connections represent polygon edges.
/// </summary>
public class PolygonSplittingGraph : GenericGraph<PolygonSplittingGraphNode>
{
    private readonly SCol.SortedDictionary<PolygonSplittingGraphNode, int> _xySortedNodes;
    private readonly SCol.List<Vector2>[] _holes;
    
    public PolygonSplittingGraph(SCol.IReadOnlyCollection<PolygonSplittingGraphNode> nodeCollection, SCol.List<Vector2>[] allIsoPerims) : base(nodeCollection)
    {
        SCol.SortedDictionary<PolygonSplittingGraphNode, int> xySortedNodes;
        this._xySortedNodes = new SCol.SortedDictionary<PolygonSplittingGraphNode, int>();
        foreach (SCol.KeyValuePair<int, PolygonSplittingGraphNode> idToNode in this.nodes)
        {
            this._xySortedNodes.Add(idToNode.Value, idToNode.Key);
        }
        this._holes = new SCol.List<Vector2>[allIsoPerims.Length - 1];
        for (int i = 1; i < allIsoPerims.Length; i++)
        { //ignore i = 0 as it's the only non-hole perimeter
            this._holes[i - 1] = allIsoPerims[i];
        }
    }

    /// <summary>
    /// Find the minimum cycles of the polygon represented by this class.
    /// Can be used to get both chordless polygons and rectangles.
    /// </summary>
    /// <returns>Return a List of Lists of Vector2s, that describes the min cycles of this polygon.</returns>
    public SCol.List<RectangularPolygon> GetMinCycles()
    {
        SCol.List<ConnectedNodeGroup> connectedGroups = this._GetConnectedGroups();
        connectedGroups.Sort();
        SCol.SortedDictionary<ConnectedNodeGroup, SCol.HashSet<int>> idsContainedInGroup = this._StoreNestedGroups(connectedGroups);
        //             2.6.5 Finding the 'Minimum Cycles' of each Node Cover using BFS.
        //             2.6.6 Checking which of a Node Cover's Minimum Cycle contains the Outer Perimeter of its children in the
        //                   Tree of Node Covers.
        //             2.6.7 Denoting each Minimum Cycle as being a chord-less polygon IF it is not a hole, and denoting the
        //                   Outer Perimeter of any Node Covers contained within a Minimum Cycle as its hole(s) (it's still
        //                   a chord-less polygon, just with a hole(s)).
        var minCycles = new SCol.List<RectangularPolygon>();
        foreach (ConnectedNodeGroup connectedGroup in connectedGroups)
        {
            minCycles.AddRange(this._PartitionConnectedNodeGroup(connectedGroup, idsContainedInGroup, connectedGroups));
        }
        
        foreach (RectangularPolygon poly in minCycles)
        {
            foreach (Vector2 outerVertex in poly.outerPerim)
            {
                GD.PrintS("outer vertex: " + outerVertex);
            }
            foreach (SCol.List<Vector2> hole in poly.holes)
            {
                GD.PrintS("hole: " + hole);
            }
            GD.PrintS();
        }
        return minCycles;
    }

    /// <summary>
    /// Finds groups of connected nodes and returns them as a list.
    /// </summary>
    /// <returns>A list of connected node groups in the polygon represented by this class.</returns>
    private SCol.List<ConnectedNodeGroup> _GetConnectedGroups()
    {
        var connectedGroups = new SCol.List<ConnectedNodeGroup>();
        var visited = new SCol.HashSet<PolygonSplittingGraphNode>();
        int id = 0;
        
        foreach (PolygonSplittingGraphNode node in this.nodes.Values)
        {
            if (visited.Contains(node)) continue;
            SCol.List<PolygonSplittingGraphNode> connectedToNode = this.GetDFSCover(new SCol.List<PolygonSplittingGraphNode>{node});
            var groupNodes = new SCol.SortedList<int, PolygonSplittingGraphNode>();
            foreach (PolygonSplittingGraphNode connectedNode in connectedToNode)
            {
                visited.Add(connectedNode);
                groupNodes.Add(connectedNode.id, connectedNode);
            }
            connectedGroups.Add(new ConnectedNodeGroup(id, groupNodes, this.adjMatrix));
            id++;
        }
        return connectedGroups;
    }

    /// <summary>
    /// Creates a dictionary that stores IDs of ConnectedNodeGroups contained within each ConnectedNodeGroup.
    /// </summary>
    /// <param name="connectedGroups">List of ConnectedNodeGroups, sorted in order of area size, descending.</param>
    /// <returns></returns>
    private SCol.SortedDictionary<ConnectedNodeGroup, SCol.HashSet<int>> _StoreNestedGroups(SCol.List<ConnectedNodeGroup> connectedGroups)
    {
        var idsContainedInGroup = new SCol.SortedDictionary<ConnectedNodeGroup, SCol.HashSet<int>>();
        for (int i = 0; i < connectedGroups.Count; i++)
        {
            ConnectedNodeGroup thisGroup = connectedGroups[i];
            idsContainedInGroup.Add(thisGroup, new SCol.HashSet<int>());
            for (int j = 0; j < connectedGroups.Count; j++)
            {
                ConnectedNodeGroup otherGroup = connectedGroups[j];
                if (i != j && GeometryFuncs.IsPolyInPoly(thisGroup.GetOuterPerimAsVectorArray(),
                    otherGroup.GetOuterPerimAsVectorArray()))
                {   
                    idsContainedInGroup[thisGroup].Add(otherGroup.id);
                }
            }
        }
        return this._SimplifyNestedGroups(idsContainedInGroup);
    }

    /// <summary>
    ///Simplify the <param>idsConnectedInGroup></param> dictionary, which stores info on which groups are contained
    ///in others, like so:
    ///idsConnectedInGroup[A] = [B C D]
    ///where group A contains B, C, and D.
    ///    However, if D is also contained within B, then the dictionary is redundant and
    ///should be simplified to:
    ///idsConnectedInGroup[A] = [B C]
    ///idsConnectedInGroup[B] = [D]
    /// </summary>
    /// <param name="idsContainedInGroup"></param>
    /// <returns></returns>
    private SCol.SortedDictionary<ConnectedNodeGroup, SCol.HashSet<int>> _SimplifyNestedGroups(
        SCol.SortedDictionary<ConnectedNodeGroup, SCol.HashSet<int>> idsContainedInGroup)
    {
        var simplifiedIdsContainedInGroup = new SCol.SortedDictionary<ConnectedNodeGroup, SCol.HashSet<int>>(idsContainedInGroup);
        foreach (ConnectedNodeGroup outsideGroup in simplifiedIdsContainedInGroup.Keys)
        {
            SCol.HashSet<int> connectedGroupIDs = idsContainedInGroup[outsideGroup];
            foreach (int insideGroupID in connectedGroupIDs)
            {
                ConnectedNodeGroup insideGroup =
                    idsContainedInGroup.Keys.First(x => x.id == insideGroupID);
                simplifiedIdsContainedInGroup[outsideGroup].ExceptWith(idsContainedInGroup[insideGroup]);
            }
        }
        return simplifiedIdsContainedInGroup;
    }

    /// <summary>
    /// Partitions a connected node group by:
    ///     1. Repeatedly running BFS on its nodes to find cycles and removing edges for the next BFS until no more
    ///        cycles can be found.
    ///         1.1 Note that the BFS only runs on nodes with two or less VALID EDGES.  Edges are only removed IFF either
    ///             of its vertices has two or less valid edges.  This ensures we do not remove an edge that could be used
    ///             twice, for example two adjacent cycles that share an edge.
    ///     2. The cycle is added to a list of perimeters (AKA a list of nodes) IFF it is NOT a hole.
    /// </summary>
    /// <param name="connectedGroup">ConnectedNodeGroup that is being partitioned.</param>
    /// <param name="idsContainedInGroup">The IDs of the ConnectedNodeGroups contained within <param>connectedGroup</param></param>
    /// <param name="connectedGroups">List of all ConnectedNodeGroups.</param>
    /// <returns></returns>
    private SCol.List<RectangularPolygon> _PartitionConnectedNodeGroup(ConnectedNodeGroup connectedGroup,
        SCol.SortedDictionary<ConnectedNodeGroup, SCol.HashSet<int>> idsContainedInGroup,
        SCol.List<ConnectedNodeGroup> connectedGroups)
    {
        var partitions = new SCol.List<RectangularPolygon>();
        SCol.Dictionary<PolygonSplittingGraphNode, SCol.HashSet<int>> removedEdges =
            connectedGroup.nodes.Values.ToDictionary(node => node, node => new SCol.HashSet<int>());

        SCol.KeyValuePair<int, PolygonSplittingGraphNode> selectedIdNodePair;
        while ((selectedIdNodePair = connectedGroup.nodes.FirstOrDefault(x =>
                this._GetNumOfNodesValidEdges(connectedGroup, x.Value, removedEdges[x.Value]) == 2)).Value != null) //forgive me lord for the sin i have commi
        { //select a node with 2 or less valid edges, and break the loop if none can be found
            try
            {
                SCol.List<PolygonSplittingGraphNode> cycle =
                    this._BFSToFindCycle(selectedIdNodePair.Value, connectedGroup, removedEdges);
                removedEdges = this._UpdateRemovedEdges(cycle, connectedGroup, removedEdges);
                if (this._IsCycleHole(cycle)) continue;
                partitions.Add(_FinalisePartition(cycle, connectedGroup, connectedGroups, idsContainedInGroup));
            }
            catch (ArgumentOutOfRangeException e)
            {
                GD.PrintS(e.Message);
            }
        }

        foreach (PolygonSplittingGraphNode node in connectedGroup.nodes.Values)
        {
            GD.PrintS("for node with id: " + node.id + " at coord: " + node.x + ", " + node.y + ", valid edges remaining: " + this._GetNumOfNodesValidEdges(connectedGroup, node, removedEdges[node]));
        }
        
        return partitions;
    }
    
    /// <summary>
    /// Counts the number of valid edges a vertex has, given that:
    ///     1. It is within a ConnectedNodeGroup and can only connect to other vertices within that group
    ///     2. Edges that have been removed (and are in <param>removedIDs</param>) are not counted 
    /// </summary>
    /// <param name="connectedGroup">Group that the vertex is in.</param>
    /// <param name="node">The node (vertex) in question.</param>
    /// <param name="removedIDs">Set of IDs of vertices that this vertex cannot travel to, AKA removed edges.</param>
    /// <returns>Number of valid edges the input vertex can travel to.</returns>
    private int _GetNumOfNodesValidEdges(ConnectedNodeGroup connectedGroup, PolygonSplittingGraphNode node, 
                                         SCol.HashSet<int> removedIDs)
    {
        int count = 0;
        for (int i = 0; i < this.adjMatrix.GetLength(1); i++)
        {
            if (this.adjMatrix[node.id, i] == 1 && connectedGroup.ContainsNode(this.nodes[i]) && !removedIDs.Contains(i))
            {
                count++;
                //GD.PrintS("found conn at from " + node.id + " to node with id: " + i + " at coord: " + connectedGroup.nodes[i].x + ", " + connectedGroup.nodes[i].y);
            }
        }
        return count;
    }

    /// <summary>
    /// Of a <param>startNode</param> with only two edges, removes one (denoted U->start) and uses BFS to find the
    /// shortest path from start->U (because our graph is undirected and unweighted). The min cycle is then the BFSd
    /// path from start->U + the removed edge U->start.
    /// This is guaranteed to work for PolygonSplittingGraph as all edges are strictly orthogonal and at no point will
    /// there ever be an absence of nodes with exactly two edges (until all nodes have been removed).
    /// </summary>
    /// <param name="startNode">Node that the BFS starts from.</param>
    /// <param name="connectedNodeGroup">Connected Group that we are limited to searching within.</param>
    /// <param name="removedEdges">Edges that have been removed and cannot be taken.</param>
    /// <returns>A list of Vector2s containing a cycle from <param>startNode</param> back to itself. As per the list
    /// convention list[start] == list[end].</returns>
    private SCol.List<PolygonSplittingGraphNode> _BFSToFindCycle(PolygonSplittingGraphNode startNode, ConnectedNodeGroup connectedNodeGroup,
                                               SCol.Dictionary<PolygonSplittingGraphNode, SCol.HashSet<int>> removedEdges)
    {
        var discovered = new SCol.HashSet<int>();
        var queueOfIDs = new SCol.Queue<int>();
        SCol.Dictionary<int, int> parent = connectedNodeGroup.nodes.Values.ToDictionary(node => node.id, node => -1);
        queueOfIDs.Enqueue(startNode.id);
        
        //remove one of startNode's two edges (temporarily just for this method)
        int removeID = -1;
        for (int i = 0; i < this.adjMatrix.GetLength(1); i++)
        {
            if (this.adjMatrix[startNode.id, i] == 1 && !removedEdges[startNode].Contains(i))
            {
                removeID = i;
                break;
            }
        }
        if (removeID == -1) throw new ArgumentOutOfRangeException("Despite this method, " + nameof(this._BFSToFindCycle) + " being called strictly with a starting node has two edges, those edges could not be found.");
        
        while (queueOfIDs.Count > 0)
        {
            int id = queueOfIDs.Dequeue();
            PolygonSplittingGraphNode node = this.nodes[id];
            if (id == removeID) break;
            for (int i = 0; i < this.adjMatrix.GetLength(1); i++)
            {
                PolygonSplittingGraphNode neighbourNode = this.nodes[i];
                if (!connectedNodeGroup.ContainsNode(neighbourNode) || removedEdges[node].Contains(i) || this.adjMatrix[id,i] == 0) continue;
                if (id == startNode.id && i == removeID || id == removeID && i == startNode.id) continue; //ignore removed edges
                if (!discovered.Contains(i) && parent[id] != i)
                {
                    discovered.Add(i);
                    parent[i] = id;
                    queueOfIDs.Enqueue(i);
                }
            }
        }
        return this._ExtractCycle(parent, startNode, removeID);
    }

    private SCol.List<PolygonSplittingGraphNode> _ExtractCycle(SCol.Dictionary<int, int> parent, PolygonSplittingGraphNode startNode, int removeID)
    {
        var cycle = new SCol.List<PolygonSplittingGraphNode>();
        int prevID = removeID;
        do
        {
            PolygonSplittingGraphNode node = this.nodes[prevID];
            cycle.Add(node);
            prevID = parent[prevID];
        } while (prevID != -1);
        cycle.Reverse();
        cycle.Add(startNode);
        //cycle.ForEach(node => GD.PrintS("extracted cycle node is at coords: " + node.x + ", " + node.y));
        cycle.Reverse();
        return cycle;
    }

    /// <summary>
    /// Uses a cycle obtained by BFS to update the <param>removedEdges</param> dictionary. An edge in the cycle is only
    /// added as a removed edge IFF either of its vertexes have two or less valid connections remaining.
    /// </summary>
    /// <param name="cycle">Cycle discovered by BFS.</param>
    /// <param name="connectedGroup">Connected group of nodes.</param>
    /// <param name="removedEdges">Dictionary of edges that have been removed.</param>
    /// <returns>An updated dictionary of removed edges with the edges in cycle added if valid.</returns>
    private SCol.Dictionary<PolygonSplittingGraphNode, SCol.HashSet<int>> _UpdateRemovedEdges(
                SCol.List<PolygonSplittingGraphNode> cycle, ConnectedNodeGroup connectedGroup,
                SCol.Dictionary<PolygonSplittingGraphNode, SCol.HashSet<int>> removedEdges)
    {
        var updatedRemovedEdges = new SCol.Dictionary<PolygonSplittingGraphNode, SCol.HashSet<int>>();
        foreach (PolygonSplittingGraphNode node in removedEdges.Keys)
        {
            updatedRemovedEdges[node] = new SCol.HashSet<int>();
            foreach (int id in removedEdges[node])
            {
                updatedRemovedEdges[node].Add(id);
            }
        }

        for (int i = 0; i < cycle.Count; i++)
        {
            PolygonSplittingGraphNode thisNode = cycle[i];
            PolygonSplittingGraphNode nextNode = cycle[(i + 1) % cycle.Count];
            if (Equals(thisNode, nextNode)) continue;
            if (this._GetNumOfNodesValidEdges(connectedGroup, thisNode, removedEdges[thisNode]) <= 2 ||
                this._GetNumOfNodesValidEdges(connectedGroup, nextNode, removedEdges[nextNode]) <= 2)
            {
                //IFF either vertex of an edge has only two or less valid connections remaining
                GD.PrintS("removed edge between node + " + thisNode.id + " at " + thisNode.x + ", " + thisNode.y + " which had # remaining edges: " + this._GetNumOfNodesValidEdges(connectedGroup, thisNode, removedEdges[thisNode]) +  " and node " + nextNode.id + " at " + nextNode.x + ", " + nextNode.y + " which had # remaining edges: " + this._GetNumOfNodesValidEdges(connectedGroup, nextNode, removedEdges[nextNode]));
                updatedRemovedEdges[thisNode].Add(nextNode.id);
                updatedRemovedEdges[nextNode].Add(thisNode.id);
            }
        }
        return updatedRemovedEdges;
    }

    /// <summary>
    /// Checks if the cycle is a hole.
    /// </summary>
    /// <param name="cycle">Cycle discovered from BFS></param>
    /// <returns>True if the cycle is a hole, false otherwise.</returns>
    private bool _IsCycleHole(SCol.List<PolygonSplittingGraphNode> cycle)
    {
        var cycleAsVectorArray = new Vector2[cycle.Count];
        for (int i = 0; i < cycle.Count; i++)
        {
            PolygonSplittingGraphNode node = cycle[i];
            cycleAsVectorArray[i] = new Vector2(node.x, node.y);
        }
        return this._holes.Any(hole => GeometryFuncs.ArePolysIdentical(cycleAsVectorArray, hole.ToArray()));
    }

    /// <summary>
    /// Final step in partitioning a ConnectedNodeGroup, by converting it into a RectangularPolygon and checking if
    /// the partition contains any holes (AKA the outerPerim of any ConnectedNodeGroups its parent ConnectedNodeGroup
    /// contains).
    /// </summary>
    /// <param name="cycle">Cycle discovered from BFS, AKA partition of ConnectedNodeGroup.></param>
    /// <param name="connectedGroup">The ConnectedNodeGroup <param>cycle</param> was derived from.</param>
    /// <param name="connectedGroups">A list of all ConnectedNodeGroups.</param>
    /// <param name="idsContainedInGroup">The IDs of the ConnectedNodeGroups contained within <param>connectedGroup</param></param>
    /// <returns></returns>
    private static RectangularPolygon _FinalisePartition(SCol.List<PolygonSplittingGraphNode> cycle, ConnectedNodeGroup connectedGroup,
                                                 SCol.List<ConnectedNodeGroup> connectedGroups, 
                                                 SCol.SortedDictionary<ConnectedNodeGroup, SCol.HashSet<int>> idsContainedInGroup)
    {
        var cycleAsVectorArray = new Vector2[cycle.Count];
        for (int i = 0; i < cycle.Count; i++)
        {
            PolygonSplittingGraphNode node = cycle[i];
            cycleAsVectorArray[i] = new Vector2(node.x, node.y);
        }

        var holes = new SCol.List<SCol.List<Vector2>>();
        foreach (int connectedGroupID in idsContainedInGroup[connectedGroup])
        {
            ConnectedNodeGroup groupInside = connectedGroups.Find(group => group.id == connectedGroupID);
            if (GeometryFuncs.IsPolyInPoly(groupInside.GetOuterPerimAsVectorArray(), cycleAsVectorArray))
            {
                holes.Add(groupInside.GetOuterPerimAsVectorArray().ToList());
            }
        }
        return new RectangularPolygon(cycleAsVectorArray, holes.ToArray());
    }
}
}