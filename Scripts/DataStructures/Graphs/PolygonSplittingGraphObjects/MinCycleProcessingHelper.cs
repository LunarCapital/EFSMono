using System;
using System.Collections.Generic;
using System.Linq;
using EFSMono.Scripts.Autoload;
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
    /// <param name="nodes">List of all nodes.</param>
    /// <param name="holes">Holes of the polygon that was made into a PolygonSplittingGraph.</param>
    /// <returns></returns>
    public static List<ChordlessPolygon> PartitionConnectedNodeGroup(ConnectedNodeGroup connectedGroup,
                                                                SortedDictionary<int, HashSet<int>> idsContainedInGroup,
                                                                SortedList<int, ConnectedNodeGroup> connectedGroups,
                                                                SortedList<int, PolygonSplittingGraphNode> nodes,
                                                                List<Vector2>[] holes)
    {
        var outerPerimCycle = new List<PolygonSplittingGraphNode>();
        var polyCycles = new List<List<PolygonSplittingGraphNode>>();
        var holeCycles = new List<List<PolygonSplittingGraphNode>>();
        
        List<List<PolygonSplittingGraphNode>> allFaces = _GetAllFaces(connectedGroup);
        var uniqueFaces = new List<List<Vector2>>();
        foreach (List<PolygonSplittingGraphNode> newFace in allFaces)
        {
            //construct Vector2[] or List<Vector2> describing face perim in Vector2s
            var newFacePerim = new Vector2[newFace.Count];
            for (int i = 0; i < newFace.Count; i++)
            {
                PolygonSplittingGraphNode node = newFace[i];
                newFacePerim[i] = new Vector2(node.x, node.y);
            }
            
            bool newFaceUnique = true;
            foreach (List<Vector2> uniqueFace in uniqueFaces)
            {
                if (GeometryFuncs.ArePolysIdentical(uniqueFace.ToArray(), newFacePerim))
                {
                    newFaceUnique = false;
                    break;
                }
            }

            if (newFaceUnique)
            {
                uniqueFaces.Add(newFacePerim.ToList());
                if (IsCycleHole(newFacePerim, holes))
                {
                    holeCycles.Add(newFace);
                }
                else if (IsCycleOuterPerim(newFacePerim, connectedGroup.outerPerimNodes))
                {
                    outerPerimCycle.AddRange(newFace);
                }
                else if (IsCycleComplex(newFacePerim))
                {
                    continue;
                }
                else
                {
                    polyCycles.Add(newFace);
                }
            }
        }

        List<ChordlessPolygon> innerHoles = holeCycles.Select(_FinaliseInnerHole).ToList();
        var partitions = new List<ChordlessPolygon>();
        foreach (List<PolygonSplittingGraphNode> polyCycle in polyCycles)
        {
            partitions.Add(_FinalisePartition(polyCycle, connectedGroup, connectedGroups,
                                                   idsContainedInGroup, nodes, innerHoles));
        }
        
        if (partitions.Count == 0 && outerPerimCycle.Count > 0) //planar face that represents the outer perim is only relevant IFF there are no other (non-hole) faces
            partitions.Add(_FinalisePartition(outerPerimCycle, connectedGroup, connectedGroups,
                                                   idsContainedInGroup, nodes, innerHoles));
        return partitions;
    }

    /// <summary>
    /// Given the input <param>connectedGroup</param>, gets all faces by running the following algorithm for EACH
    /// CONNECTION for EVERY NODE:
    ///     1. Ensure that all nodes have their connections ordered counter-clockwise (already done in ConnectedNodeGroup
    ///        constructor)
    ///     2. Given some starting node S, travel any of its connections, which we will define as C
    ///     3. From C, pick its connection that is next in order from the previous node (in this case, S)
    ///     4. Repeat until we get back to S, and now we have a face.
    /// This method gets all faces, many of which will be duplicates.
    /// A better explanation is from here: https://math.stackexchange.com/questions/8140/find-all-cycles-faces-in-a-graph
    /// </summary>
    /// <param name="connectedGroup"></param>
    /// <returns></returns>
    private static List<List<PolygonSplittingGraphNode>> _GetAllFaces(ConnectedNodeGroup connectedGroup)
    {
        var allFaces = new List<List<PolygonSplittingGraphNode>>();
        foreach (PolygonSplittingGraphNode node in connectedGroup.nodes.Values)
        {
            foreach (int connID in node.connectedNodeIDs)
            {
                if (!connectedGroup.nodes.ContainsKey(connID)) continue; //thanks to bridges a node may have a connection to a node that is not present in its node group (bridges split node groups)
                var face = new List<PolygonSplittingGraphNode>();
                face.Add(node);
                int prevID = node.id;
                int currentID = connID;
                do
                {
                    face.Add(connectedGroup.nodes[currentID]);
                    int nextID;
                    do
                    { //in case of bridge, keep getting nextID if it does not exist in connectedGroup
                        nextID = connectedGroup.GetCCWNextNodeID(prevID, currentID);
                    } while (!connectedGroup.nodes.ContainsKey(nextID));
                    prevID = currentID;
                    currentID = nextID;
                } while (prevID != node.id);
                //face.Add(node);
                allFaces.Add(face);
            }
        }
        return allFaces;
    }
    
    /// <summary>
    /// Creates a ChordlessPolygon with just an outer perimeter and flags it as a hole.
    /// </summary>
    /// <param name="cycle"></param>
    /// <returns>A ChordlessPolygon flagged as a hole, just with an outer perimeter.</returns>
    private static ChordlessPolygon _FinaliseInnerHole(List<PolygonSplittingGraphNode> cycle)
    {
        var cyclePerim = new Vector2[cycle.Count];
        for (int i = 0; i < cycle.Count; i++)
        {
            PolygonSplittingGraphNode node = cycle[i];
            cyclePerim[i] = new Vector2(node.x, node.y);
        }
        var holePolygon = new ChordlessPolygon(cyclePerim, new List<Vector2>[0],
                                        new Dictionary<Vector2, HashSet<Vector2>>(), true);
        return holePolygon;
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
    /// <param name="nodes"></param>
    /// <param name="innerHoles">Holes extracted from the same ConnectedNodeGroup.</param>
    /// <returns></returns>
    private static ChordlessPolygon _FinalisePartition(List<PolygonSplittingGraphNode> cycle, ConnectedNodeGroup connectedGroup,
                                                SortedList<int, ConnectedNodeGroup> connectedGroups, 
                                                SortedDictionary<int, HashSet<int>> idsContainedInGroup,
                                                SortedList<int, PolygonSplittingGraphNode> nodes,
                                                List<ChordlessPolygon> innerHoles)
    {
        var cycleAsVectorArray = new Vector2[cycle.Count];
        for (int i = 0; i < cycle.Count; i++)
        {
            PolygonSplittingGraphNode node = cycle[i];
            cycleAsVectorArray[i] = new Vector2(node.x, node.y);
        }
        
        var potentialHoles = new List<List<Vector2>>();
        potentialHoles.AddRange(innerHoles.Select(innerHole => innerHole.outerPerim.ToList()));
        var bridges = new Dictionary<Vector2, HashSet<Vector2>>();
        foreach (int connectedGroupID in idsContainedInGroup[connectedGroup.id])
        {
            ConnectedNodeGroup groupInside = connectedGroups[connectedGroupID];
            potentialHoles.Add(groupInside.outerPerimSimplified.ToList());
            
            foreach (int nodeID in groupInside.bridges.Keys)
            {
                if (!cycle.Exists(x => x.id == nodeID)) continue;
                var nodeCoord = new Vector2(nodes[nodeID].x, nodes[nodeID].y);
                foreach (int neighbourID in groupInside.bridges[nodeID])
                {
                    if (!cycle.Exists(x => x.id == neighbourID)) continue;
                    var neighbourCoord = new Vector2(nodes[neighbourID].x, nodes[neighbourID].y);
                    if (!bridges.ContainsKey(nodeCoord)) bridges[nodeCoord] = new HashSet<Vector2>();
                    bridges[nodeCoord].Add(neighbourCoord);
                }
            }
        }
        return new ChordlessPolygon(cycleAsVectorArray, potentialHoles.ToArray(), bridges, false);
    }
}
}