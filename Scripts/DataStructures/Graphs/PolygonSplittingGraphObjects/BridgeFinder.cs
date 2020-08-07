using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects
{
/// <summary>
/// A class dedicated to finding bridges in the graph before ConnectedNodeGraphs are built.
/// </summary>
public static class BridgeFinder
{
    private const int NIL = -1;
    private static int _time;

    /// <summary>
    /// Finds all bridges in a graph using Tarjan's bridge-finding algorithm.
    /// </summary>
    /// <param name="allNodes"></param>
    /// <param name="adjMatrix"></param>
    /// <param name="removedEdges">Edges that are removed and cannot be used in the algorithm.</param>
    /// <returns></returns>
    public static Dictionary<int, HashSet<int>> GetBridges(SortedList<int, PolygonSplittingGraphNode> allNodes,
                                                           int[,] adjMatrix,
                                                           Dictionary<int, HashSet<int>> removedEdges = null)
    {
        if (removedEdges == null) removedEdges = new Dictionary<int, HashSet<int>>();
        if (allNodes.Count == 0) return new Dictionary<int, HashSet<int>>();
        var visited = new HashSet<int>();
        int numOfNodes = allNodes.Keys.Max() + 1;
        
        var preorder = new int[numOfNodes]; //on what iteration of the DFS was the node discovered
        var low = new int[numOfNodes]; // low[v] is the lowest vertex reachable from a subtree with root v
        var parent = new int[numOfNodes];
        _time = 0;

        var bridges = new Dictionary<int, HashSet<int>>();
        for (int i = 0; i < numOfNodes; i++)
        {
            parent[i] = NIL;
            bridges[i] = new HashSet<int>();
        }

        for (int i = 0; i < numOfNodes; i++)
        {
            if (!allNodes.ContainsKey(i)) continue;
            GD.PrintS("node with id: " + i + " is at coords: " + allNodes[i].x + ", " + allNodes[i].y);
        }
        
        for (int i = 0; i < numOfNodes; i++)
        {
            if (!allNodes.ContainsKey(i)) continue;
            if (!visited.Contains(i))
            {
                GD.PrintS("brdige dfsing at node : " + i + ", with coords: " + allNodes[i].x + ", " + allNodes[i].y);
                _BridgeDFS(i, visited, preorder, low, parent, adjMatrix, bridges, removedEdges);
            }
        }
        return bridges;
    }

    /// <summary>
    /// A recursive hell that finds bridges by:
    ///     1. Labelling every node with a number signifying 'when' it was discovered (preorder)
    ///     2. Maintaining, for every node N, its lowest reachable vertex from a subtree with vertex N.
    /// A bridge is confirmed between some node U and some node V if V's lowest reachable vertex is further down the tree
    /// than U.
    /// Credit to: https://www.geeksforgeeks.org/bridge-in-a-graph/
    /// for the algorithm which i pretty much shamelessly stole.
    /// </summary>
    /// <param name="visitedID"></param>
    /// <param name="visited"></param>
    /// <param name="preorder"></param>
    /// <param name="low"></param>
    /// <param name="parent"></param>
    /// <param name="adjMatrix"></param>
    /// <param name="bridges"></param>
    /// <param name="removedEdges"></param>
    private static void _BridgeDFS(int visitedID, HashSet<int> visited, int[] preorder, int[] low, int[] parent,
                                   int[,] adjMatrix, Dictionary<int, HashSet<int>> bridges,
                                   Dictionary<int, HashSet<int>> removedEdges)
    {
        visited.Add(visitedID);
        preorder[visitedID] = low[visitedID] = ++_time;
        for (int neighbourID = 0; neighbourID < adjMatrix.GetLength(1); neighbourID++)
        {
            if (adjMatrix[visitedID, neighbourID] == 0) continue;
            if (removedEdges.ContainsKey(visitedID))
            {
                if (removedEdges[visitedID].Contains(neighbourID)) continue;
            }

            if (!visited.Contains(neighbourID))
            {
                parent[neighbourID] = visitedID;
                _BridgeDFS(neighbourID, visited, preorder, low, parent, adjMatrix, bridges, removedEdges);
                low[visitedID] = Math.Min(low[visitedID], low[neighbourID]);
                GD.PrintS("node: " + visitedID + " has pre: " + preorder[visitedID] + " and low: " + low[visitedID]);
                if (low[neighbourID] > preorder[visitedID])
                { //FOUND BRIDGE, as lowest vertex reachable from neighbourID is further down the tree than visitedID
                    GD.PrintS("BRIDGE FOUND");
                    bridges[visitedID].Add(neighbourID);
                    bridges[neighbourID].Add(visitedID);
                }
            }
            else if (neighbourID != parent[visitedID])
            {
                GD.PrintS("choosing low[visitedID] for vID: " + visitedID + " and nID: " + neighbourID + " with choices: " + low[visitedID] + " and " + preorder[neighbourID]);
                low[visitedID] = Math.Min(low[visitedID], preorder[neighbourID]);
            }
        }
    }
}
}