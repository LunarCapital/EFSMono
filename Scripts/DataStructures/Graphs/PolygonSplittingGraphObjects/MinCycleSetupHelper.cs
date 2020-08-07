using Godot;
using System;
using System.Linq;
using EFSMono.Scripts.Autoload;
using System.Collections.Generic;

namespace EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects
{
/// <summary>
/// Helper class for PolygonSplittingGraph that contains methods related to setup of MinCycle extraction, AKA
/// manipulation/storage of ConnectedNodeGroups so that the main class doesn't have 1 billion lines.
/// </summary>
public static class MinCycleSetupHelper
{
    
    /// <summary>
    /// Finds groups of connected nodes and returns them as a list.
    /// </summary>
    /// <returns>A list of connected node groups in the polygon represented by this class.</returns>
    public static SortedList<int, ConnectedNodeGroup> GetConnectedGroups(SortedList<int, PolygonSplittingGraphNode> nodes, int[,] adjMatrix,
                                                              Func<List<PolygonSplittingGraphNode>,
                                                                   Dictionary<int, HashSet<int>>,
                                                                   List<PolygonSplittingGraphNode>> getDFSCover)
    {
        var connectedGroups = new SortedList<int, ConnectedNodeGroup>();
        var visited = new HashSet<PolygonSplittingGraphNode>();
        Dictionary<int, HashSet<int>> bridges = BridgeFinder.GetBridges(nodes, adjMatrix);
        GD.PrintS("bridge algo fin");
        foreach (int uID in bridges.Keys)
        {
            foreach (int vID in bridges[uID])
            {
                GD.PrintS("bridge present with IDs from " + uID + " to " + vID + ", coords are: " + nodes[uID].x + ", " + nodes[uID].y + " to " + nodes[vID].x + ", " + nodes[vID].y);
            }
        }
        int id = 0;
        
        foreach (PolygonSplittingGraphNode node in nodes.Values)
        {
            if (visited.Contains(node)) continue;
            List<PolygonSplittingGraphNode> connectedToNode = getDFSCover(new List<PolygonSplittingGraphNode>{node}, bridges);
            var groupNodes = new SortedList<int, PolygonSplittingGraphNode>();
            var groupBridges = new Dictionary<int, HashSet<int>>();
            GD.PrintS("DFS'd node with id: " + node.id + ", at " + node.x + ", " + node.y);
            foreach (PolygonSplittingGraphNode connectedNode in connectedToNode)
            {
                GD.PrintS("DFS's group which contains: " + connectedNode.id + ", at " + connectedNode.x + ", " + connectedNode.y);
                visited.Add(connectedNode);
                groupNodes.Add(connectedNode.id, connectedNode);
                if (bridges.ContainsKey(connectedNode.id))
                {
                    groupBridges[connectedNode.id] = bridges[connectedNode.id];
                }
            }
            connectedGroups.Add(id, new ConnectedNodeGroup(id, groupNodes, adjMatrix, groupBridges));
            id++;
        }
        return connectedGroups;
    }

    /// <summary>
    /// Creates a dictionary that stores IDs of ConnectedNodeGroups contained within each ConnectedNodeGroup.
    /// </summary>
    /// <param name="connectedGroups">List of ConnectedNodeGroups, sorted in order of area size, descending.</param>
    /// <returns></returns>
    public static SortedDictionary<int, HashSet<int>> StoreNestedGroups(SortedList<int, ConnectedNodeGroup> connectedGroups)
    {
        var idsContainedInGroup = new SortedDictionary<int, HashSet<int>>();
        for (int i = 0; i < connectedGroups.Count; i++)
        {
            ConnectedNodeGroup thisGroup = connectedGroups[i];
            idsContainedInGroup.Add(thisGroup.id, new HashSet<int>());
            for (int j = 0; j < connectedGroups.Count; j++)
            {
                ConnectedNodeGroup otherGroup = connectedGroups[j];
                if (i != j && thisGroup.IsOtherGroupInThisGroup(otherGroup))
                {   
                    idsContainedInGroup[thisGroup.id].Add(otherGroup.id);
                }
            }
        }
        return _SimplifyNestedGroups(connectedGroups, idsContainedInGroup);
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
    private static SortedDictionary<int, HashSet<int>> _SimplifyNestedGroups(SortedList<int, ConnectedNodeGroup> connectedGroups,
                                                                             SortedDictionary<int, HashSet<int>> idsContainedInGroup)
    {
        var simplifiedIdsContainedInGroup = new SortedDictionary<int, HashSet<int>>();
        foreach (int outsideGroupID in idsContainedInGroup.Keys)
        {
            simplifiedIdsContainedInGroup[outsideGroupID] = new HashSet<int>();
            simplifiedIdsContainedInGroup[outsideGroupID].UnionWith(idsContainedInGroup[outsideGroupID]);
            HashSet<int> connectedGroupIDs = idsContainedInGroup[outsideGroupID];
            foreach (int insideGroupID in connectedGroupIDs)
            {
                simplifiedIdsContainedInGroup[outsideGroupID].ExceptWith(idsContainedInGroup[insideGroupID]);
            }
        }
        return simplifiedIdsContainedInGroup;
    }

    public static Dictionary<PolygonSplittingGraphNode, HashSet<int>> InitRemovedEdges(SortedList<int, PolygonSplittingGraphNode> nodes,
                                                                                       SortedList<int, ConnectedNodeGroup> nodeGroups)
    {
        Dictionary<PolygonSplittingGraphNode, HashSet<int>> removedEdges =
            nodes.Values.ToDictionary(node => node, node => new HashSet<int>());

        foreach (ConnectedNodeGroup group in nodeGroups.Values)
        {
            foreach (int bridgeID in group.bridges.Keys)
            {
                PolygonSplittingGraphNode bridgeNode = nodes[bridgeID];
                foreach (int bridgeConn in group.bridges[bridgeID])
                {
                    removedEdges[bridgeNode].Add(bridgeConn);
                }
            }
        }
        
        return removedEdges;
    }

}
}