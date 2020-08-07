using System;
using System.Linq;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using Godot;
using System.Collections.Generic;
using Helper = EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects.PolygonSplittingGraphGroupHelper;

namespace EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects
{
/// <summary>
/// A graph specifically designed to split a polygon, which can be useful for two purpose:
///     1. Splitting a polygon-with-chords into chordless polygons
///     2. Splitting chordless polygons into rectangles
/// This graph's nodes represent polygon vertices, and connections represent polygon edges.
///
/// i am now aware after hours of googling that this class would get me a position in OOP prison, so some self-notes for future:
///     1. Constructors with logic can be replaced by GoF builder (? not sure about this one)
///     2. could create a static class to handle some of the logic like IsCycleHole and GetNumOfNodesValidEdges
///     3. FinalisePartition could be changed into a builder for ChordlessPolygon?
///     4. i don't think GetMinCycles can be simplified because it's genuinely an abomination but i'll keep thinking about it
///     5. i FOROGT about bridges holy shit that cost me so much mess
///     6. this class plays extremely badly with nodes with 4-connections (one in each direction), so i think it's necessary to
///        split these into two 2-conn nodes BEFORE CREATING THIS CLASS (also because I don't want to touch this thing anymore).
/// </summary>    
public class PolygonSplittingGraph : GenericGraph<PolygonSplittingGraphNode>
{
    private readonly SortedDictionary<PolygonSplittingGraphNode, int> _xySortedNodes;
    private readonly List<Vector2>[] _holes;

    public PolygonSplittingGraph(IReadOnlyCollection<PolygonSplittingGraphNode> nodeCollection, List<List<Vector2>> holes = null) : base(nodeCollection)
    {
        if (holes == null) holes = new List<List<Vector2>>();
        this._holes = holes.ToArray();
    }
    
    public PolygonSplittingGraph(IReadOnlyCollection<PolygonSplittingGraphNode> nodeCollection, List<Vector2>[] allIsoPerims) : base(nodeCollection)
    {
        SortedDictionary<PolygonSplittingGraphNode, int> xySortedNodes;
        this._xySortedNodes = new SortedDictionary<PolygonSplittingGraphNode, int>();
        foreach (KeyValuePair<int, PolygonSplittingGraphNode> idToNode in this.nodes)
        {
            this._xySortedNodes.Add(idToNode.Value, idToNode.Key);
        }
        this._holes = new List<Vector2>[allIsoPerims.Length - 1];
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
    public List<ChordlessPolygon> GetMinCycles()
    {
        SortedList<int, ConnectedNodeGroup> connectedGroups = Helper.GetConnectedGroups(this.nodes, this.adjMatrix, this.GetDFSCover);
        SortedDictionary<int, HashSet<int>> idsContainedInGroup = Helper.StoreNestedGroups(connectedGroups);

        foreach (ConnectedNodeGroup node in connectedGroups.Values)
        {
            GD.PrintS("connectedgroup with id: " + node.id + " has perim: ");
            foreach (Vector2 vertex in node.outerPerimSimplified)
            {
                GD.PrintS(vertex);
            }
            foreach (int id in node.nodes.Keys)
            {
                GD.PrintS("id in conn group: " + id);
            }
        }

        foreach (int outsideID in idsContainedInGroup.Keys)
        {
            foreach (int insideID in idsContainedInGroup[outsideID])
            {
                GD.PrintS("group " + insideID + " is inside " + outsideID);
            }
        }
        
        var minCycles = new List<ChordlessPolygon>();
        foreach (ConnectedNodeGroup connectedGroup in connectedGroups.Values)
        {
            minCycles.AddRange(this._PartitionConnectedNodeGroup(connectedGroup, idsContainedInGroup, connectedGroups));
        }
        return minCycles;
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
    private List<ChordlessPolygon> _PartitionConnectedNodeGroup(ConnectedNodeGroup connectedGroup,
                                                                SortedDictionary<int, HashSet<int>> idsContainedInGroup,
                                                                SortedList<int, ConnectedNodeGroup> connectedGroups)
    {
        var partitions = new List<ChordlessPolygon>();
        Dictionary<PolygonSplittingGraphNode, HashSet<int>> removedEdges = Helper.InitRemovedEdges(this.nodes, connectedGroups);

        KeyValuePair<int, PolygonSplittingGraphNode> selectedIdNodePair;
        while ((selectedIdNodePair = connectedGroup.nodes.FirstOrDefault(x => 
                Helper.GetNumOfNodesValidEdges(connectedGroup, x.Value, removedEdges[x.Value], 
                                               this.adjMatrix,this.nodes) == 2)).Value != null) //forgive me lord for the sin i have commi
        { //select a node with 2 or less valid edges, and break the loop if none can be found
            try
            {
                List<PolygonSplittingGraphNode> cycle =
                    this._BFSToFindCycle(selectedIdNodePair.Value, connectedGroup, removedEdges);
                removedEdges = this._UpdateRemovedEdges(cycle, connectedGroup, removedEdges);
                removedEdges = Helper.AddNewBridgesToRemovedEdges(connectedGroup, this.adjMatrix, removedEdges);
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
            GD.PrintS("for node with id: " + node.id + " at coord: " + node.x + ", " + node.y + ", valid edges remaining: " + Helper.GetNumOfNodesValidEdges(connectedGroup, node, removedEdges[node], this.adjMatrix, this.nodes));
        }

        return partitions;
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
    private List<PolygonSplittingGraphNode> _BFSToFindCycle(PolygonSplittingGraphNode startNode, ConnectedNodeGroup connectedNodeGroup,
                                               Dictionary<PolygonSplittingGraphNode, HashSet<int>> removedEdges)
    {
        var discovered = new HashSet<int>();
        var queueOfIDs = new Queue<int>();
        Dictionary<int, int> parent = connectedNodeGroup.nodes.Values.ToDictionary(node => node.id, node => -1);
        queueOfIDs.Enqueue(startNode.id);
        GD.PrintS("Enqueued Starting Node with id: " + startNode.id + " at location: " + startNode.x + ", " + startNode.y);
        
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

    private List<PolygonSplittingGraphNode> _ExtractCycle(Dictionary<int, int> parent, PolygonSplittingGraphNode startNode, int removeID)
    {
        foreach (int key in parent.Keys)
        {
            GD.PrintS("node key: " + key + " maps to " + parent[key]);
        }
        
        var cycle = new List<PolygonSplittingGraphNode>();
        int prevID = removeID;
        do
        {
            PolygonSplittingGraphNode node = this.nodes[prevID];
            cycle.Add(node);
            GD.PrintS("Added node with id: " + node.id + " at coord: " + node.x + ", " + node.y);
            prevID = parent[prevID];
        } while (prevID != -1);
        cycle.Reverse();
        cycle.Add(startNode);
        cycle.ForEach(node => GD.PrintS("extracted cycle node is at coords: " + node.x + ", " + node.y));
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
    private Dictionary<PolygonSplittingGraphNode, HashSet<int>> _UpdateRemovedEdges(
                List<PolygonSplittingGraphNode> cycle, ConnectedNodeGroup connectedGroup,
                Dictionary<PolygonSplittingGraphNode, HashSet<int>> removedEdges)
    {
        var updatedRemovedEdges = new Dictionary<PolygonSplittingGraphNode, HashSet<int>>();
        foreach (PolygonSplittingGraphNode node in removedEdges.Keys)
        {
            updatedRemovedEdges[node] = new HashSet<int>();
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
            if (Helper.GetNumOfNodesValidEdges(connectedGroup, thisNode, removedEdges[thisNode], this.adjMatrix, this.nodes) <= 2 ||
                Helper.GetNumOfNodesValidEdges(connectedGroup, nextNode, removedEdges[nextNode], this.adjMatrix, this.nodes) <= 2)
            {
                //IFF either vertex of an edge has only two or less valid connections remaining
                GD.PrintS("removed edge between node + " + thisNode.id + " at " + thisNode.x + ", " + thisNode.y + " which had # remaining edges: " + Helper.GetNumOfNodesValidEdges(connectedGroup, thisNode, removedEdges[thisNode], this.adjMatrix, this.nodes) +  " and node " + nextNode.id + " at " + nextNode.x + ", " + nextNode.y + " which had # remaining edges: " + Helper.GetNumOfNodesValidEdges(connectedGroup, nextNode, removedEdges[nextNode], this.adjMatrix, this.nodes));
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
    private bool _IsCycleHole(List<PolygonSplittingGraphNode> cycle)
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
    private ChordlessPolygon _FinalisePartition(List<PolygonSplittingGraphNode> cycle, ConnectedNodeGroup connectedGroup,
                                                       SortedList<int, ConnectedNodeGroup> connectedGroups, 
                                                       SortedDictionary<int, HashSet<int>> idsContainedInGroup)
    {
        var cycleAsVectorArray = new Vector2[cycle.Count];
        for (int i = 0; i < cycle.Count; i++)
        {
            PolygonSplittingGraphNode node = cycle[i];
            cycleAsVectorArray[i] = new Vector2(node.x, node.y);
        }
        
        var potentialHoles = new List<List<Vector2>>();
        var bridges = new Dictionary<Vector2, HashSet<Vector2>>();
        foreach (int connectedGroupID in idsContainedInGroup[connectedGroup.id])
        {
            ConnectedNodeGroup groupInside = connectedGroups[connectedGroupID];
            potentialHoles.Add(groupInside.outerPerimSimplified.ToList());
            
            foreach (int nodeID in groupInside.bridges.Keys)
            { 
                var nodeCoord = new Vector2(this.nodes[nodeID].x, this.nodes[nodeID].y);
                foreach (int neighbourID in groupInside.bridges[nodeID])
                {
                    var neighbourCoord = new Vector2(this.nodes[neighbourID].x, this.nodes[neighbourID].y);
                    if (!bridges.ContainsKey(nodeCoord)) bridges[nodeCoord] = new HashSet<Vector2>();
                    bridges[nodeCoord].Add(neighbourCoord);
                }
            }
        }
        return new ChordlessPolygon(cycleAsVectorArray, potentialHoles.ToArray(), bridges);
    }
}
}