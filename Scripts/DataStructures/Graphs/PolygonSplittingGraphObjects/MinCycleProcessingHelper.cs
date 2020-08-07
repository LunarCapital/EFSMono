using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using EFSMono.Scripts.DataStructures.Geometry;
using static EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects.MinCycleManagementHelper;

namespace EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects
{
/// <summary>
/// A helper class for PolygonSplittingGraph that contains the processing for MinCycle extraction, AKA BFSing, finding
/// which node to search to get the smallest cycle, etc.
/// </summary>
public static class MinCycleProcessingHelper
{
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
    public static List<ChordlessPolygon> PartitionConnectedNodeGroup(ConnectedNodeGroup connectedGroup,
                                                                SortedDictionary<int, HashSet<int>> idsContainedInGroup,
                                                                SortedList<int, ConnectedNodeGroup> connectedGroups,
                                                                Dictionary<PolygonSplittingGraphNode, HashSet<int>> removedEdges,
                                                                SortedList<int, PolygonSplittingGraphNode> nodes,
                                                                int[,] adjMatrix, List<Vector2>[] holes)
    {
        var partitions = new List<ChordlessPolygon>();

        List<PolygonSplittingGraphNode> validNodes;
        while ((validNodes = GetAllNodesWithOnlyTwoEdges(connectedGroup, removedEdges, adjMatrix)).Count > 0)
        { //select a node with 2 or less valid edges, and break the loop if none can be found
            try
            {
                var potentialCycles = new List<List<PolygonSplittingGraphNode>>();
                foreach (PolygonSplittingGraphNode node in validNodes)
                {
                    GD.PrintS("GETTING CYCLE INCLUSIVE OF NODE WITH ID " + node.id + " AT: " + node.x + ", " + node.y);
                    List<PolygonSplittingGraphNode> cycle = _BFSToFindCycle(node, connectedGroup, removedEdges, nodes, adjMatrix);
                    potentialCycles.Add(cycle);
                }
                GD.PrintS("FINISHED CYCLING FOR VALID NODES");
                List<PolygonSplittingGraphNode> smallestCycle = GetSmallestCycle(potentialCycles);
                foreach (PolygonSplittingGraphNode node in smallestCycle)
                {
                    GD.PrintS("smallest cycle contains vertex: " + node.x + ", " + node.y);
                }
                removedEdges = UpdateRemovedEdges(adjMatrix, smallestCycle, connectedGroup, removedEdges);
                removedEdges = AddNewBridgesToRemovedEdges(connectedGroup, adjMatrix, removedEdges);
                if (IsCycleHole(smallestCycle, holes)) continue;
                partitions.Add(_FinalisePartition(smallestCycle, connectedGroup, connectedGroups, idsContainedInGroup, nodes));
            }
            catch (ArgumentOutOfRangeException e)
            {
                GD.PrintS(e.Message);
            }
        }

        foreach (PolygonSplittingGraphNode node in connectedGroup.nodes.Values)
        {
            GD.PrintS("for node with id: " + node.id + " at coord: " + node.x + ", " + node.y + ", valid edges remaining: " + GetNumOfNodesValidEdges(connectedGroup, node, removedEdges[node], adjMatrix));
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
    private static List<PolygonSplittingGraphNode> _BFSToFindCycle(PolygonSplittingGraphNode startNode,
                                                    ConnectedNodeGroup connectedNodeGroup,
                                                    Dictionary<PolygonSplittingGraphNode, HashSet<int>> removedEdges,
                                                    SortedList<int, PolygonSplittingGraphNode> nodes,
                                                    int[,] adjMatrix)
    {
        var discovered = new HashSet<int>();
        var queueOfIDs = new Queue<int>();
        Dictionary<int, int> parent = connectedNodeGroup.nodes.Values.ToDictionary(node => node.id, node => -1);
        queueOfIDs.Enqueue(startNode.id);
        GD.PrintS("Enqueued Starting Node with id: " + startNode.id + " at location: " + startNode.x + ", " + startNode.y);
        
        //remove one of startNode's two edges (temporarily just for this method)
        int removeID = -1;
        for (int i = 0; i < adjMatrix.GetLength(1); i++)
        {
            if (adjMatrix[startNode.id, i] == 1 && !removedEdges[startNode].Contains(i))
            {
                removeID = i;
                break;
            }
        }
        if (removeID == -1) throw new ArgumentOutOfRangeException("Despite this method, " + nameof(_BFSToFindCycle) + " being called strictly with a starting node has two edges, those edges could not be found.");
        
        while (queueOfIDs.Count > 0)
        {
            int id = queueOfIDs.Dequeue();
            PolygonSplittingGraphNode node = nodes[id];
            if (id == removeID) break;
            for (int i = 0; i < adjMatrix.GetLength(1); i++)
            {
                PolygonSplittingGraphNode neighbourNode = nodes[i];
                if (!connectedNodeGroup.ContainsNode(neighbourNode) || removedEdges[node].Contains(i) || adjMatrix[id,i] == 0) continue;
                if (id == startNode.id && i == removeID || id == removeID && i == startNode.id) continue; //ignore removed edges
                if (!discovered.Contains(i) && parent[id] != i)
                {
                    discovered.Add(i);
                    parent[i] = id;
                    queueOfIDs.Enqueue(i);
                }
            }
        }
        return _ExtractCycle(parent, removeID, startNode, nodes);
    }

    private static List<PolygonSplittingGraphNode> _ExtractCycle(Dictionary<int, int> parent, int removeID,
                                                                 PolygonSplittingGraphNode startNode,
                                                                 SortedList<int, PolygonSplittingGraphNode> nodes)
    {
        foreach (int key in parent.Keys)
        {
            GD.PrintS("node key: " + key + " maps to " + parent[key]);
        }
        
        var cycle = new List<PolygonSplittingGraphNode>();
        int prevID = removeID;
        do
        {
            PolygonSplittingGraphNode node = nodes[prevID];
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
    /// Final step in partitioning a ConnectedNodeGroup, by converting it into a RectangularPolygon and checking if
    /// the partition contains any holes (AKA the outerPerim of any ConnectedNodeGroups its parent ConnectedNodeGroup
    /// contains).
    /// </summary>
    /// <param name="cycle">Cycle discovered from BFS, AKA partition of ConnectedNodeGroup.></param>
    /// <param name="connectedGroup">The ConnectedNodeGroup <param>cycle</param> was derived from.</param>
    /// <param name="connectedGroups">A list of all ConnectedNodeGroups.</param>
    /// <param name="idsContainedInGroup">The IDs of the ConnectedNodeGroups contained within <param>connectedGroup</param></param>
    /// <returns></returns>
    private static ChordlessPolygon _FinalisePartition(List<PolygonSplittingGraphNode> cycle, ConnectedNodeGroup connectedGroup,
                                                SortedList<int, ConnectedNodeGroup> connectedGroups, 
                                                SortedDictionary<int, HashSet<int>> idsContainedInGroup,
                                                SortedList<int, PolygonSplittingGraphNode> nodes)
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
                var nodeCoord = new Vector2(nodes[nodeID].x, nodes[nodeID].y);
                foreach (int neighbourID in groupInside.bridges[nodeID])
                {
                    var neighbourCoord = new Vector2(nodes[neighbourID].x, nodes[neighbourID].y);
                    if (!bridges.ContainsKey(nodeCoord)) bridges[nodeCoord] = new HashSet<Vector2>();
                    bridges[nodeCoord].Add(neighbourCoord);
                }
            }
        }
        return new ChordlessPolygon(cycleAsVectorArray, potentialHoles.ToArray(), bridges);
    }
}
}