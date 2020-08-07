using System;
using System.Linq;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using Godot;
using System.Collections.Generic;
using static EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects.MinCycleSetupHelper;
using static EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects.MinCycleProcessingHelper;
using static EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects.MinCycleManagementHelper;

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
        SortedList<int, ConnectedNodeGroup> connectedGroups = GetConnectedGroups(this.nodes, this.adjMatrix, this.GetDFSCover);
        SortedDictionary<int, HashSet<int>> idsContainedInGroup = StoreNestedGroups(connectedGroups);

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
            Dictionary<PolygonSplittingGraphNode, HashSet<int>> removedEdges = InitRemovedEdges(this.nodes, connectedGroups);
            minCycles.AddRange(PartitionConnectedNodeGroup(connectedGroup, idsContainedInGroup, connectedGroups,
                                                                     removedEdges, this.nodes, this.adjMatrix, this._holes));
        }
        return minCycles;
    }
}
}