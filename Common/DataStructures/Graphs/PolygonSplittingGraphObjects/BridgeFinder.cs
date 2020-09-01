using System;
using System.Collections.Generic;

namespace EFSMono.Common.DataStructures.Graphs.PolygonSplittingGraphObjects
{
    /// <summary>
    /// A class dedicated to finding bridges in the graph before ConnectedNodeGraphs are built.
    /// </summary>
    public static class BridgeFinder
    {
        private const int NIL = -1;

        private static SortedList<int, PolygonSplittingGraphNode> _allNodes;
        private static HashSet<int> _visited;
        private static int[] _preorder;
        private static int[] _low;
        private static int[] _parent;
        private static Dictionary<int, HashSet<int>> _bridges;
        private static int _time;

        /// <summary>
        /// Finds all bridges in a graph using Tarjan's bridge-finding algorithm.
        /// </summary>
        /// <param name="allNodes"></param>
        /// <returns></returns>
        public static Dictionary<int, HashSet<int>> GetBridges(SortedList<int, PolygonSplittingGraphNode> allNodes)
        {
            if (allNodes is null) throw new ArgumentNullException(nameof(allNodes));

            if (allNodes.Count == 0) return new Dictionary<int, HashSet<int>>();
            _allNodes = allNodes;
            _visited = new HashSet<int>();
            int numOfNodes = allNodes.Count;

            _preorder = new int[numOfNodes]; //on what iteration of the DFS was the node discovered
            _low = new int[numOfNodes]; // low[v] is the lowest vertex reachable from a subtree with root v
            _parent = new int[numOfNodes];
            _time = 0;
            _bridges = new Dictionary<int, HashSet<int>>();

            for (int i = 0; i < numOfNodes; i++)
            {
                _parent[i] = NIL;
                _bridges[i] = new HashSet<int>();
            }

            for (int i = 0; i < numOfNodes; i++)
            {
                if (!_visited.Contains(i))
                    _BridgeDFS(allNodes[i]);
            }
            return _bridges;
        }

        /// <summary>
        /// A recursive DFS that finds bridges by:
        ///     1. Labelling every node with a number signifying 'when' it was discovered (preorder)
        ///     2. Maintaining, for every node N, its lowest reachable vertex from a subtree with vertex N.
        /// A bridge is confirmed between some node U and some node V if V's lowest reachable vertex is further down the tree
        /// than U.
        /// Credit to: https://www.geeksforgeeks.org/bridge-in-a-graph/
        /// for the algorithm which i pretty much shamelessly stole.
        /// </summary>
        /// <param name="node">Node that this method is visiting.</param>
        private static void _BridgeDFS(PolygonSplittingGraphNode node)
        {
            int visitedID = node.id;
            _visited.Add(visitedID);
            _preorder[visitedID] = _low[visitedID] = ++_time;
            foreach (int neighbourID in node.connectedNodeIDs)
            {
                if (!_visited.Contains(neighbourID))
                {
                    _parent[neighbourID] = visitedID;
                    _BridgeDFS(_allNodes[neighbourID]);
                    _low[visitedID] = Math.Min(_low[visitedID], _low[neighbourID]);
                    if (_low[neighbourID] > _preorder[visitedID])
                    { //FOUND BRIDGE, as lowest vertex reachable from neighbourID is further down the tree than visitedID
                        _bridges[visitedID].Add(neighbourID);
                        _bridges[neighbourID].Add(visitedID);
                    }
                }
                else if (neighbourID != _parent[visitedID])
                {
                    _low[visitedID] = Math.Min(_low[visitedID], _preorder[neighbourID]);
                }
            }
        }
    }
}