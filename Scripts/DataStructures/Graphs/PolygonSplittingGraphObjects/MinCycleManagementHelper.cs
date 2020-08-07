using System.Collections.Generic;
using System.Linq;
using EFSMono.Scripts.Autoload;
using Godot;

namespace EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects
{
/// <summary>
/// Helper class for PolygonSplittingGraph that handles the management of MinCycle extraction, AKA updating which edges
/// can no longer be travelled, whether any bridges are present in the graph and need to be removed, etc.
/// </summary>
public static class MinCycleManagementHelper
{
    /// <summary>
    /// Finds all nodes that have exactly two valid ledges.
    /// </summary>
    /// <param name="connectedGroup"></param>
    /// <param name="removedEdges"></param>
    /// <param name="adjMatrix"></param>
    /// <returns>List of nodes with exactly two valid ledges.</returns>
    public static List<PolygonSplittingGraphNode> GetAllNodesWithOnlyTwoEdges(ConnectedNodeGroup connectedGroup,
                                               Dictionary<PolygonSplittingGraphNode, HashSet<int>> removedEdges,
                                               int[,] adjMatrix)
    {
        var twoEdgeNodes = new List<PolygonSplittingGraphNode>();
        foreach (PolygonSplittingGraphNode node in connectedGroup.nodes.Values)
        {
            if (GetNumOfNodesValidEdges(connectedGroup, node, removedEdges[node], adjMatrix) == 2)
            {
                twoEdgeNodes.Add(node);
            }
        }
        return twoEdgeNodes;
    }
    
    /// <summary>
    /// Counts the number of valid edges a vertex has, given that:
    ///     1. It is within a ConnectedNodeGroup and can only connect to other vertices within that group
    ///     2. Edges that have been removed (and are in <param>removedIDs</param>) are not counted 
    /// </summary>
    /// <param name="connectedGroup">Group that the vertex is in.</param>
    /// <param name="node">The node (vertex) in question.</param>
    /// <param name="removedIDs">Set of IDs of vertices that this vertex cannot travel to, AKA removed edges.</param>
    /// <param name="adjMatrix"></param>
    /// <param name="nodes"></param>
    /// <returns>Number of valid edges the input vertex can travel to.</returns>
    public static int GetNumOfNodesValidEdges(ConnectedNodeGroup connectedGroup, PolygonSplittingGraphNode node, 
                                              HashSet<int> removedIDs, int[,] adjMatrix)
    {
        int count = 0;
        for (int i = 0; i < adjMatrix.GetLength(1); i++)
        {
            if (adjMatrix[node.id, i] == 1 && connectedGroup.nodes.ContainsKey(i) && !removedIDs.Contains(i))
            {
                count++;
                GD.PrintS("found conn at from " + node.id + " to node with id: " + i + " at coord: " + connectedGroup.nodes[i].x + ", " + connectedGroup.nodes[i].y);
            }
        }
        return count;
    }
    
    /// <summary>
    /// Uses a cycle obtained by BFS to update the <param>removedEdges</param> dictionary. An edge in the cycle is only
    /// added as a removed edge IFF either of its vertexes have two or less valid connections remaining.
    /// </summary>
    /// <param name="cycle">Cycle discovered by BFS.</param>
    /// <param name="connectedGroup">Connected group of nodes.</param>
    /// <param name="removedEdges">Dictionary of edges that have been removed.</param>
    /// <returns>An updated dictionary of removed edges with the edges in cycle added if valid.</returns>
    public static Dictionary<PolygonSplittingGraphNode, HashSet<int>> UpdateRemovedEdges(int[,] adjMatrix,
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
            if (GetNumOfNodesValidEdges(connectedGroup, thisNode, removedEdges[thisNode], adjMatrix) <= 2 ||
                GetNumOfNodesValidEdges(connectedGroup, nextNode, removedEdges[nextNode], adjMatrix) <= 2)
            {
                //IFF either vertex of an edge has only two or less valid connections remaining
                GD.PrintS("removed edge between node + " + thisNode.id + " at " + thisNode.x + ", " + thisNode.y + " which had # remaining edges: " + GetNumOfNodesValidEdges(connectedGroup, thisNode, removedEdges[thisNode], adjMatrix) +  " and node " + nextNode.id + " at " + nextNode.x + ", " + nextNode.y + " which had # remaining edges: " + GetNumOfNodesValidEdges(connectedGroup, nextNode, removedEdges[nextNode], adjMatrix));
                updatedRemovedEdges[thisNode].Add(nextNode.id);
                updatedRemovedEdges[nextNode].Add(thisNode.id);
            }
        }
        return updatedRemovedEdges;
    }
        
    /// <summary>
    /// Searches for bridges within the remaining graph and removes them for the next cycle extraction.
    /// </summary>
    /// <param name="connectedGroup"></param>
    /// <param name="adjMatrix"></param>
    /// <param name="removedEdges"></param>
    /// <returns></returns>
    public static Dictionary<PolygonSplittingGraphNode, HashSet<int>> AddNewBridgesToRemovedEdges(
            ConnectedNodeGroup connectedGroup, int[,] adjMatrix,
            Dictionary<PolygonSplittingGraphNode, HashSet<int>> removedEdges)
        {
            var removedEdgesIDs = new Dictionary<int, HashSet<int>>();
            foreach (PolygonSplittingGraphNode node in removedEdges.Keys)
            {
                removedEdgesIDs[node.id] = removedEdges[node];
            }
        
            Dictionary<int, HashSet<int>> bridges = BridgeFinder.GetBridges(connectedGroup.nodes, adjMatrix, removedEdgesIDs);
            var updatedRemovedEdges = new Dictionary<PolygonSplittingGraphNode, HashSet<int>>(removedEdges);
            foreach (int bridgeID in bridges.Keys)
            {
                if (!connectedGroup.nodes.ContainsKey(bridgeID)) continue;
                foreach (int neighbourID in bridges[bridgeID])
                {
                    updatedRemovedEdges[connectedGroup.nodes[bridgeID]].Add(neighbourID);
                    GD.PrintS("removed bridge during cycle with IDs " + bridgeID + " to " + neighbourID + " with coords: " + connectedGroup.nodes[bridgeID].x + ", " + connectedGroup.nodes[bridgeID].y + ", " + connectedGroup.nodes[neighbourID].x + ", " + connectedGroup.nodes[neighbourID].y);
                }
            }
            return updatedRemovedEdges;
        }

    /// <summary>
    /// Given a list of multiple cycles, returns the one with the smallest area.
    /// </summary>
    /// <param name="allCycles">List of lists of PolygonSplittingGraphNodes, each list representing a cycle.</param>
    /// <returns>The smallest cycle in the input list.</returns>
    public static List<PolygonSplittingGraphNode> GetSmallestCycle(List<List<PolygonSplittingGraphNode>> allCycles)
    {
        GD.PrintS("starting getSMALLESTCYCLE, # of cycles: " + allCycles.Count);
        int smallCycleID = 0;
        float minArea = float.PositiveInfinity;
        for (int cycleID = 0; cycleID < allCycles.Count; cycleID++)
        {
            GD.PrintS("cycleID: " + cycleID);
            List<PolygonSplittingGraphNode> cycle = allCycles[cycleID];
            var perim = new Vector2[cycle.Count];
            for (int i = 0; i < cycle.Count; i++)
            {
                GD.PrintS("node id in cycle: " + i);
                PolygonSplittingGraphNode node = cycle[i];
                perim[i] = new Vector2(node.x, node.y);
            }
            float perimArea = GeometryFuncs.GetAreaOfPolygon(perim);
            if (perimArea < minArea)
            {
                smallCycleID = cycleID;
                minArea = perimArea;
            }
        }
        return allCycles[smallCycleID];
    }
    
    /// <summary>
    /// Checks if the cycle is a hole.
    /// </summary>
    /// <param name="cycle">Cycle discovered from BFS></param>
    /// <param name="holes">List of holes (which are a list of Vector2s).</param>
    /// <returns>True if the cycle is a hole, false otherwise.</returns>
    public static bool IsCycleHole(List<PolygonSplittingGraphNode> cycle, List<Vector2>[] holes)
    {
        var cycleAsVectorArray = new Vector2[cycle.Count];
        for (int i = 0; i < cycle.Count; i++)
        {
            PolygonSplittingGraphNode node = cycle[i];
            cycleAsVectorArray[i] = new Vector2(node.x, node.y);
        }
        return holes.Any(hole => GeometryFuncs.ArePolysIdentical(cycleAsVectorArray, hole.ToArray()));
    }
}
}