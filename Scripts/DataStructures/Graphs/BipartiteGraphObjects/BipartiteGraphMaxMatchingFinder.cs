using System.Linq;
using SCol = System.Collections.Generic;

namespace EFSMono.Scripts.DataStructures.Graphs.BipartiteGraphObjects
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
    public static SCol.Dictionary<BipartiteGraphNode, BipartiteGraphNode> GetMaxMatching(this BipartiteGraph bipartiteGraph)
    {
        var maxMatching = new SCol.Dictionary<BipartiteGraphNode, BipartiteGraphNode>();
        var networkNodesIDs = new SCol.List<int>(bipartiteGraph.nodes.Keys);
        int sourceID = networkNodesIDs.Count;
        int sinkID = sourceID + 1;
        networkNodesIDs.Add(sourceID);
        networkNodesIDs.Add(sinkID);
        
        int[,] capacityNetwork = _ConstructCapacityNetwork(networkNodesIDs, bipartiteGraph.leftNodeIDs,
            bipartiteGraph.rightNodeIDs, bipartiteGraph.adjMatrix, sourceID, sinkID);
        var flowNetwork = new int[networkNodesIDs.Count, networkNodesIDs.Count];
        var residualNetwork = (int[,]) capacityNetwork.Clone();

        SCol.List<int> augmentingPath;
        while ((augmentingPath = _GetAugmentingPath(residualNetwork, sourceID, sinkID)).Count > 1)
        {
            flowNetwork = _UpdateFlowNetwork(augmentingPath, flowNetwork);
            
        }
        
        return maxMatching;
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
    private static int[,] _ConstructCapacityNetwork(SCol.IReadOnlyCollection<int> networkNodeIDs,
                                                    SCol.HashSet<int> leftNodeIDs,
                                                    SCol.HashSet<int> rightNodeIDs,
                                                    int[,] adjMatrix, int sourceID, int sinkID)
    {
        var capacityNetwork = new int[networkNodeIDs.Count, networkNodeIDs.Count];
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
                    foreach (int rightNodeID in rightNodeIDs.Where(rightNodeID => adjMatrix[id, rightNodeID] == 1))
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
    private static SCol.List<int> _GetAugmentingPath(int[,] residualNetwork, int sourceID, int sinkID)
    {
        var queue = new SCol.Queue<int>();
        var discovered = new SCol.HashSet<int>();
        var parent = new int[sinkID + 1];
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
    private static SCol.List<int> _ExtractAugmentingPath(SCol.IReadOnlyList<int> parent, int sinkID)
    {
        var augmentingPath = new SCol.List<int> {sinkID};
        int prev = parent[sinkID];
        while (prev != -1)
        {
            augmentingPath.Add(prev);
            prev = parent[prev];
        }
        augmentingPath.Reverse();
        return parent[sinkID] != -1 ? augmentingPath : new SCol.List<int>();
    }

    /// <summary>
    /// Update the flow network by adding flow to the augmenting path.
    /// </summary>
    /// <param name="augmentingPath">Augmenting Path.</param>
    /// <param name="flowNetwork">Flow network.</param>
    /// <returns>The updated flow network with added flow to the augmenting path.</returns>
    private static int[,] _UpdateFlowNetwork(SCol.IReadOnlyList<int> augmentingPath, int[,] flowNetwork)
    {
        var updatedFlowNetwork = (int[,]) flowNetwork.Clone();
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
        var updatedResidualNetwork = (int[,]) residualNetwork.Clone();
        for (int i = 0; i <= sinkID; i++)
        {
            for (int j = 0; j <= sinkID; j++)
            {
                int residualFlow = capacityNetwork[i, j] - flowNetwork[i, j];
                updatedResidualNetwork[i, j] = (residualFlow > 0) ? residualFlow : 0;
            } //TODO: do i actually update the backward edges?
        }
        return updatedResidualNetwork;
    }
}
}