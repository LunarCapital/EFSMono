﻿using System.Linq;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using EFSMono.Scripts.DataStructures.Graphs.BipartiteGraphObjects;
using EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects;
using EFSMono.Scripts.SystemModules.PhysicsControllerModule.RIDConstructors.PolygonPartitioning.PolygonObjects;
using Godot;
using SCol = System.Collections.Generic;

namespace EFSMono.Scripts.SystemModules.PhysicsControllerModule.RIDConstructors.PolygonPartitioning
{
/// <summary>
/// A class intended to decompose 'complex' polygons into rectangles.  'Complex' in this context refers to
/// having chords.  Decomposition is done by partitioning the initial complex polygon into simpler, chord-less polygons
/// (which may or may not have holes) first, then partitioning those into rectangles via the separate class
/// ChordlessPolygonDecomposer.
/// </summary>
public static class ComplexPolygonDecomposer
{
    /// <summary>
    /// Decomposes a complex polygon described by the input <param>allIsoPerims</param> into the minimum number of
    /// rectangles.
    /// </summary>
    /// <param name="allIsoPerims">Array of lists of Vector2s.  Each list describes a perimeter, whether it be
    /// the complex polygon's outer perimeters or holes (the 0'th index is always the outer perimeter).</param>
    /// <returns>A list of lists of Vector2s.  Each list describes a rectangle in coordinates that follow the
    /// isometric axis (and need to be converted back to the cartesian axis elsewhere).</returns>
    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    public static SCol.List<SCol.List<Vector2>> DecomposeComplexPolygonToRectangles(this SCol.List<Vector2>[] allIsoPerims)
    {
        var allRectangles = new SCol.List<SCol.List<Vector2>>();
        foreach (SCol.List<Vector2> perim in allIsoPerims)
        {
            GD.PrintS("Perim:");
            foreach (Vector2 vertex in perim)
            {
                GD.PrintS(vertex.x + ", " + vertex.y);
            }
        }

        SCol.List<SCol.List<Vector2>> chordlessPolygons = _ComplexToChordlessPolygons(allIsoPerims);
        //TODO split chordless polygons into rectangles
        
        return allRectangles;
    }

    /// <summary>
    /// Partitions a complex polygon (with chords and holes) into a group of chordless polygons.
    /// </summary>
    /// <param name="allIsoPerims">Array of lists of Vector2s, each describing a perimeter of a tile group.</param>
    /// <returns>A list of lists of Vector2s, each list describing a chordless polygon.</returns>
    // ReSharper disable once ReturnTypeCanBeEnumerable.Local
    private static SCol.List<SCol.List<Vector2>> _ComplexToChordlessPolygons(SCol.List<Vector2>[] allIsoPerims)
    {
        //Give each thing its own function, except that last one it needs its own class
        
        //Find Chords
        SCol.List<Chord> chords = _FindChords(allIsoPerims);
        //Build Bipartite Graph
        SCol.Dictionary<BipartiteGraphNode, Chord> bipartiteNodeToChords = _ConvertChordsToNodes(chords);
        GD.PrintS("# of chords: " + chords.Count);
        foreach (Chord chord in chords)
        {
            GD.PrintS("Chord is from: " + chord.a + " to " + chord.b);
        }
        var bipartiteGraph = new BipartiteGraph(bipartiteNodeToChords.Keys);
        
        //Find Max Independent Set (conjugate of MVC)
        SCol.HashSet<BipartiteGraphNode> maxIndependentSet = bipartiteGraph.GetMaxIndependentSet();
        SCol.List<PolygonSplittingGraphNode> polygonSplittingNodes = _ConstructPolygonSplittingNodes(allIsoPerims, bipartiteNodeToChords, maxIndependentSet);
        
        //Extract Chordless Polygons by: (separate class)
        //         2.6 Extracting these chord-less polygons by:
        // 
        //             2.6.1 Constructing a graph out of the 'drawing', with vertices as nodes and lines/chords as edges.
        PolygonSplittingGraph polygonSplittingGraph = new PolygonSplittingGraph(polygonSplittingNodes, allIsoPerims);
        SCol.List<RectangularPolygon> minCycles = polygonSplittingGraph.GetMinCycles();
        //             2.6.2 Finding 'Node Covers' by running DFS on the graph until all nodes are visited.
        //             2.6.3 Constructing a 'tree' of Node Covers where a parent cover contains child covers, and covers that
        //                   do not contain each other are siblings.
        //             2.6.4 Finding the 'Outer Perimeter' of each Node Cover by:
        // 
        //                 2.6.4.1 Starting from the node with the smallest X and Y position.
        //                 2.6.4.2 Defining a bearing as being in the negative Y direction (as there are no nodes in that direction).
        //                 2.6.4.3 Picking the next node out of available neighbours by choosing the one with the last positive.
        //                         CCW angle change from the bearing.
        //                 2.6.4.4 Making the new bearing = direction from new node TO old node.
        //                 2.6.4.5 Repeating until the start node is reached.
        // 
        //             2.6.5 Finding the 'Minimum Cycles' of each Node Cover using BFS.
        //             2.6.6 Checking which of a Node Cover's Minimum Cycle contains the Outer Perimeter of its children in the
        //                   Tree of Node Covers.
        //             2.6.7 Denoting each Minimum Cycle as being a chord-less polygon IF it is not a hole, and denoting the
        //                   Outer Perimeter of any Node Covers contained within a Minimum Cycle as its hole(s) (it's still
        //                   a chord-less polygon, just with a hole(s)).
        return new SCol.List<SCol.List<Vector2>>(); //TODO remove this placeholder
    }

    /// <summary>
    /// Given an irregular polygon described by <param>allIsoPerims</param>, find all of its chords and
    /// return a list containing them.
    /// </summary>
    /// <param name="allIsoPerims">Array of lists of Vector2s, where each list describes a perim of a polygon, whether
    /// that perim be the polygon's outer perimeter or its hole perimeters.</param>
    /// <returns>A list of chords within <param>allIsoPerims</param>.</returns>
    private static SCol.List<Chord> _FindChords(SCol.List<Vector2>[] allIsoPerims)
    {
        var chords = new SCol.List<Chord>();
        foreach (SCol.List<Vector2> perimA in allIsoPerims)
        {
            foreach (Vector2 pointA in perimA)
            {
                foreach (SCol.List<Vector2> perimB in allIsoPerims)
                {
                    foreach (Vector2 pointB in perimB)
                    {
                        if (!_IsChordValid(pointA, pointB, allIsoPerims, chords)) continue;
                        var chord = new Chord(pointA, pointB);
                        chords.Add(chord);
                    }
                }
            }
        }
        return chords;
    }

    /// <summary>
    /// Checks if a segment between two vertexes is a valid chord, which relies on the following conditions:
    /// 0. The segment is not in the chords array already (with/without reversed points)
    /// 1. The vertices are different
    /// 2. The segment is vertical or horizontal
    /// 3. The segment does not CONTAIN a part of the polygon's outer perimeter OR hole perimeter(s).
    /// </summary>
    /// <param name="pointA"></param>
    /// <param name="pointB"></param>
    /// <param name="allIsoPerims"></param>
    /// <param name="chords"></param>
    /// <returns></returns>
    private static bool _IsChordValid(Vector2 pointA, Vector2 pointB, SCol.List<Vector2>[] allIsoPerims,
                                      SCol.IEnumerable<Chord> chords)
    {
        if (chords.Any(chord => (chord.a == pointA && chord.b == pointB) || (chord.a == pointB && chord.b == pointA)))
        { //if not already in chords array
            return false;
        }
        if (pointA == pointB) return false; //if vertices are different
        if (pointA.x != pointB.x && pointA.y != pointB.y) return false; //if the segment is vertical or horizontal
        
        foreach (SCol.List<Vector2> perims in allIsoPerims)
        { //if segment does not contain a part of the polygon's perimeter(s)
            for (int i = 0; i < perims.Count - 1; i++)
            { //i < perims.Count - 1 because perims[0] = perims[last]
                Vector2 perimVertexA = perims[i];
                Vector2 perimVertexB = perims[i + 1];
                if (GeometryFuncs.DoSegmentsOverlap(pointA, pointB, perimVertexA, perimVertexB))
                { //segment confirmed to contain part of polygon's perimeter(s)
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Use <param>chords</param> to construct Bipartite Graph Nodes and create a dictionary that maps Node -> Chords
    /// (so we can easily get which chords were 'selected' from the Max Independent Set later).
    /// </summary>
    /// <param name="chords">List of chords in polygon.</param>
    /// <returns>A dictionary that maps Bipartite Graph Nodes to Chords.</returns>
    private static SCol.Dictionary<BipartiteGraphNode, Chord> _ConvertChordsToNodes(SCol.IReadOnlyList<Chord> chords)
    {
        var bipartiteNodeToChords = new SCol.Dictionary<BipartiteGraphNode, Chord>();
        for (int i = 0; i < chords.Count; i++)
        {
            Chord chord = chords[i];
            var connectedNodeIDs = new SCol.List<int>();
            for (int j = 0; j < chords.Count; j++)
            {
                Chord comparisonChord = chords[j];
                if (j == i || comparisonChord.direction == chord.direction) continue;
                if (GeometryFuncs.DoSegmentsIntersect(chord.a, chord.b, comparisonChord.a, comparisonChord.b))
                { //chord B is connected to chord A IFF they intersect, B != A, and they have different orientations
                    connectedNodeIDs.Add(j);
                }
            }
            BipartiteGraphNode.BipartiteSide side = (chord.direction == Chord.Direction.VERTICAL) ? 
                                                    BipartiteGraphNode.BipartiteSide.LEFT :
                                                    BipartiteGraphNode.BipartiteSide.RIGHT;
            var bipartiteGraphNode = new BipartiteGraphNode(i, connectedNodeIDs, side);
            bipartiteNodeToChords.Add(bipartiteGraphNode, chord);
        }
        return bipartiteNodeToChords;
    }

    /// <summary>
    /// Given the polygon perimeters and its drawn chords, construct nodes out of each vertex and lists of each of their
    /// connected edges into a list of the class ChordSplittingGraphNode which will be used to split the polygon into
    /// chordless polygons.
    /// </summary>
    /// <param name="allIsoPerims">Array of lists describing the polygon perimeters.</param>
    /// <param name="bipartiteNodeToChords">Map of all BipartiteNodes to Chords.</param>
    /// <param name="maxIndependentSet">MIS of BipartiteNodes which contains WHICH chords we should 'draw'.</param>
    /// <returns>A list of ChordSplittingGraphNodes that describe each vertex of the polygon (with chords included).</returns>
    private static SCol.List<PolygonSplittingGraphNode> _ConstructPolygonSplittingNodes(SCol.List<Vector2>[] allIsoPerims,
                                                                                    SCol.Dictionary<BipartiteGraphNode, Chord> bipartiteNodeToChords,
                                                                                    SCol.HashSet<BipartiteGraphNode> maxIndependentSet)
    {
        var polygonSplittingNodes = new SCol.List<PolygonSplittingGraphNode>();
        var vertexToID = new SCol.Dictionary<Vector2, int>();
        int nodeID = 0;
        foreach (SCol.List<Vector2> perim in allIsoPerims)
        { //to store IDs for vertices because we need to know their connections before we can init them as graph nodes
            for (int i = 0; i < perim.Count - 1; i++)
            {
                Vector2 vertex = perim[i];
                vertexToID.Add(vertex, nodeID);
                nodeID++;
            }
        }

        SCol.SortedList<int, SCol.List<int>> nodeConnectionsByID = _FindVertexConnections(allIsoPerims, bipartiteNodeToChords, maxIndependentSet, vertexToID);
        foreach (Vector2 vertex in vertexToID.Keys)
        {
            int vertexID = vertexToID[vertex];
            var chordSplittingNode = new PolygonSplittingGraphNode(vertexID, nodeConnectionsByID[vertexID], vertex.x, vertex.y);
            polygonSplittingNodes.Add(chordSplittingNode);
        }
        return polygonSplittingNodes;
    }

    /// <summary>
    /// Given the polygon perimeters and drawn chords, constructs lists for each vertex containing the ID of every vertex
    /// that they are connected to via a drawn line (AKA edge or chord).
    /// </summary>
    /// <param name="allIsoPerims">Array of lists describing the polygon perimeters.</param>
    /// <param name="bipartiteNodeToChords">Map of all BipartiteNodes to Chords.</param>
    /// <param name="maxIndependentSet">MIS of BipartiteNodes which contains WHICH chords we should 'draw'.</param>
    /// <param name="vertexToID">Dictionary mapping vertices to an ID.</param>
    /// <returns>Lists of connected IDs for every vertex.</returns>
    private static SCol.SortedList<int, SCol.List<int>> _FindVertexConnections(SCol.List<Vector2>[] allIsoPerims,
                                                                               SCol.Dictionary<BipartiteGraphNode, Chord> bipartiteNodeToChords,
                                                                               SCol.HashSet<BipartiteGraphNode> maxIndependentSet,
                                                                               SCol.Dictionary<Vector2, int> vertexToID)
    {
        var nodeConnectionsByID = new SCol.SortedList<int, SCol.List<int>>();
        foreach (SCol.List<Vector2> perim in allIsoPerims)
        {
            for (int i = 0; i < perim.Count - 1; i++)
            { //Add polygon edges first
                var thisNodesConnections = new SCol.List<int>();
                Vector2 vertex = perim[i];
                Vector2 prevVertex;
                Vector2 nextVertex;
                if (i != 0)
                {
                    prevVertex = perim[i - 1];
                    nextVertex = perim[i + 1];
                }
                else
                {
                    prevVertex = perim[perim.Count - 2];
                    nextVertex = perim[1];
                }
                thisNodesConnections.Add(vertexToID[prevVertex]);
                thisNodesConnections.Add(vertexToID[nextVertex]);
                int vertexID = vertexToID[vertex];
                nodeConnectionsByID.Add(vertexID, thisNodesConnections);
            }
        }

        foreach (BipartiteGraphNode bipartiteNode in maxIndependentSet)
        { //add chords to connected nodes
            Chord chord = bipartiteNodeToChords[bipartiteNode];
            Vector2 vertexA = chord.a;
            Vector2 vertexB = chord.b;
            int idA = vertexToID[vertexA];
            int idB = vertexToID[vertexB];
            nodeConnectionsByID[idA].Add(idB);
            nodeConnectionsByID[idB].Add(idA);
        }
        return nodeConnectionsByID;
    }
}
}