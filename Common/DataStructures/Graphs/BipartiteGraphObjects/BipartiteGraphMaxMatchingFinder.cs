﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace EFSMono.Common.DataStructures.Graphs.BipartiteGraphObjects
{
    /// <summary>
    /// An class that contains an extension method for a BipartiteGraph that finds its Max Matching,
    /// by creating a flow network and running Ford-Fulkerson.
    /// </summary>
    public static class BipartiteGraphMaxMatchingFinder
    {
        /// <summary>
        /// Given a BipartiteGraph, finds its Max Matching by running Ford-Fulkerson.
        /// </summary>
        /// <param name="bipartiteGraph">Graph whose MM is being found.</param>
        /// <returns>Dictionary that maps left -> right nodes in the MM.</returns>
        public static Dictionary<BipartiteGraphNode, BipartiteGraphNode> GetMaxMatching(this BipartiteGraph bipartiteGraph)
        {
            if (bipartiteGraph is null) throw new ArgumentNullException(nameof(bipartiteGraph));

            var networkNodesIDs = new List<int>(bipartiteGraph.nodes.Keys);
            int sourceID = networkNodesIDs.Count;
            int sinkID = sourceID + 1;
            networkNodesIDs.Add(sourceID);
            networkNodesIDs.Add(sinkID);

            int[,] capacityNetwork = _ConstructCapacityNetwork(networkNodesIDs, bipartiteGraph.leftNodeIDs,
                bipartiteGraph.rightNodeIDs, bipartiteGraph.adjMatrix, sourceID, sinkID);
            int[,] flowNetwork = new int[networkNodesIDs.Count, networkNodesIDs.Count];
            int[,] residualNetwork = (int[,])capacityNetwork.Clone();

            List<int> augmentingPath;
            while ((augmentingPath = _GetAugmentingPath(residualNetwork, sourceID, sinkID)).Count > 1)
            {
                flowNetwork = _UpdateFlowNetwork(augmentingPath, flowNetwork);
                residualNetwork = _UpdateResidualNetwork(capacityNetwork, flowNetwork, residualNetwork, sinkID);
            }
            return _ExtractMaxMatching(flowNetwork, bipartiteGraph.leftNodeIDs,
                                       bipartiteGraph.rightNodeIDs, bipartiteGraph.nodes);
        }

        /// <summary>
        /// Construct the capacity network for Ford-Fulkerson. Source connects to all left-side nodes,
        /// left-side nodes connect to any right-side nodes that they are adjacent to in the Bipartite
        /// Graph, and right-side nodes connect to the sink.
        /// </summary>
        /// <param name="networkNodeIDs">List of IDs for nodes in this flow network.</param>
        /// <param name="leftNodeIDs">Contains IDs of left nodes.</param>
        /// <param name="rightNodeIDs">Contains IDs of right nodes.</param>
        /// <param name="adjMatrix">Adjacency Matrix of the Bipartite Graph.</param>
        /// <param name="sourceID">ID of source node.</param>
        /// <param name="sinkID">ID of sink node.</param>
        /// <returns>The capacity network, a multidimensional array.</returns>
        private static int[,] _ConstructCapacityNetwork(IReadOnlyCollection<int> networkNodeIDs,
                                                        HashSet<int> leftNodeIDs,
                                                        HashSet<int> rightNodeIDs,
                                                        ImmutableArray<ImmutableArray<int>> adjMatrix,
                                                        int sourceID, int sinkID)
        {
            int[,] capacityNetwork = new int[networkNodeIDs.Count, networkNodeIDs.Count];
            foreach (int id in networkNodeIDs)
            {
                if (id == sourceID)
                { //connect to all left nodes
                    foreach (int leftNodeID in leftNodeIDs)
                    {
                        capacityNetwork[id, leftNodeID] = 1;
                    }
                }
                else if (id == sinkID)
                { //sink connects to nothing
                    continue;
                }
                else
                {
                    if (leftNodeIDs.Contains(id))
                    { //left nodes are...
                        foreach (int rightNodeID in rightNodeIDs.Where(rightNodeID => adjMatrix[id][rightNodeID] == 1))
                        { //...connected to right side nodes
                            capacityNetwork[id, rightNodeID] = 1;
                        }
                    }
                    else
                    { //right nodes connect to sink
                        capacityNetwork[id, sinkID] = 1;
                    }
                }
            }
            return capacityNetwork;
        }

        /// <summary>
        /// Uses BFS to check if an augmenting path exists in the <param>residualNetwork</param> from source
        /// to sink and returns it as an array of IDs in forward order.
        /// </summary>
        /// <param name="residualNetwork">Residual network of flow network.</param>
        /// <param name="sourceID">ID of source node.</param>
        /// <param name="sinkID">ID of sink node.</param>
        /// <returns>Array of IDs in forward order from source to sink.</returns>
        private static List<int> _GetAugmentingPath(int[,] residualNetwork, int sourceID, int sinkID)
        {
            var queue = new Queue<int>();
            var discovered = new HashSet<int>();
            int[] parent = new int[sinkID + 1];
            for (int i = 0; i < parent.Length; i++)
            {
                parent[i] = -1;
            }
            queue.Enqueue(sourceID);
            discovered.Add(sourceID);

            while (queue.Count > 0)
            {
                int nodeID = queue.Dequeue();
                if (nodeID == sinkID) break;
                for (int i = 0; i < residualNetwork.GetLength(0); i++)
                {
                    if (residualNetwork[nodeID, i] <= 0 || discovered.Contains(i)) continue;
                    discovered.Add(i);
                    parent[i] = nodeID;
                    queue.Enqueue(i);
                }
            }
            return _ExtractAugmentingPath(parent, sinkID);
        }

        /// <summary>
        /// Extracts the augmenting path from the parent array obtained from BFSing through the residual network.
        /// </summary>
        /// <param name="parent">Int array of parents of all nodes from a BFS.</param>
        /// <param name="sinkID">ID of sink node.</param>
        /// <returns>Augmenting path to sink ID.</returns>
        private static List<int> _ExtractAugmentingPath(IReadOnlyList<int> parent, int sinkID)
        {
            var augmentingPath = new List<int> { sinkID };
            int prev = parent[sinkID];
            while (prev != -1)
            {
                augmentingPath.Add(prev);
                prev = parent[prev];
            }
            augmentingPath.Reverse();
            return parent[sinkID] != -1 ? augmentingPath : new List<int>();
        }

        /// <summary>
        /// Update the flow network by adding flow to the augmenting path.
        /// </summary>
        /// <param name="augmentingPath">Augmenting Path.</param>
        /// <param name="flowNetwork">Flow network.</param>
        /// <returns>The updated flow network with added flow to the augmenting path.</returns>
        private static int[,] _UpdateFlowNetwork(IReadOnlyList<int> augmentingPath, int[,] flowNetwork)
        {
            int[,] updatedFlowNetwork = (int[,])flowNetwork.Clone();
            for (int i = 1; i < augmentingPath.Count; i++)
            {
                int prevID = augmentingPath[i - 1];
                int currentID = augmentingPath[i];
                updatedFlowNetwork[prevID, currentID] += 1;
            }
            return updatedFlowNetwork;
        }

        /// <summary>
        /// Updates the residual network, where forward edges = (capacity - flow), and backward edges = flow.
        /// </summary>
        /// <param name="capacityNetwork">Capacity network.</param>
        /// <param name="flowNetwork">Flow network.</param>
        /// <param name="residualNetwork">Residual network.</param>
        /// <param name="sinkID">ID of sink node.</param>
        /// <returns>Updated residual network.</returns>
        private static int[,] _UpdateResidualNetwork(int[,] capacityNetwork, int[,] flowNetwork,
                                                     int[,] residualNetwork, int sinkID)
        {
            int[,] updatedResidualNetwork = (int[,])residualNetwork.Clone();
            for (int i = 0; i <= sinkID; i++)
            {
                for (int j = 0; j <= sinkID; j++)
                {
                    int residualFlow = capacityNetwork[i, j] - flowNetwork[i, j];
                    updatedResidualNetwork[i, j] = residualFlow > 0 ? residualFlow : 0;
                    updatedResidualNetwork[j, i] = residualFlow > 0 ? flowNetwork[i, j] : updatedResidualNetwork[j, i];
                }
            }
            return updatedResidualNetwork;
        }

        /// <summary>
        /// Extracts the max matching from the flow network.
        /// </summary>
        /// <param name="flowNetwork">Flow Network.</param>
        /// <param name="leftNodeIDs">Set of left node IDs.</param>
        /// <param name="rightNodeIDs">Set of right node IDs.</param>
        /// <param name="bipartiteNodes">SortedList of BipartiteGraphNodes.</param>
        /// <returns>The max matching, in the form of a dictionary that maps left to right nodes.</returns>
        private static Dictionary<BipartiteGraphNode, BipartiteGraphNode> _ExtractMaxMatching(int[,] flowNetwork,
                                                                                              HashSet<int> leftNodeIDs,
                                                                                              HashSet<int> rightNodeIDs,
                                                                                              IReadOnlyDictionary<int, BipartiteGraphNode> bipartiteNodes)
        {
            var maxMatching = new Dictionary<BipartiteGraphNode, BipartiteGraphNode>();
            foreach (int leftNodeID in leftNodeIDs)
            {
                foreach (int rightNodeID in rightNodeIDs)
                {
                    if (flowNetwork[leftNodeID, rightNodeID] <= 0) continue;
                    BipartiteGraphNode leftNode = bipartiteNodes[leftNodeID];
                    BipartiteGraphNode rightNode = bipartiteNodes[rightNodeID];
                    maxMatching.Add(leftNode, rightNode);
                }
            }
            return maxMatching;
        }
    }
}