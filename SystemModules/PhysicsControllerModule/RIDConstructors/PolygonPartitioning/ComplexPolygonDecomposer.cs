using System;
using System.Collections.Generic;
using System.Linq;
using EFSMono.Common.Autoload;
using EFSMono.Common.DataStructures.Geometry;
using EFSMono.Common.DataStructures.Graphs.BipartiteGraphObjects;
using EFSMono.Common.DataStructures.Graphs.PolygonSplittingGraphObjects;
using Godot;

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
        /// rectangles (actually a lie, decomposes them into chordless polygons, which is decomposed into rectangles in
        /// another class).
        /// </summary>
        /// <param name="allIsoPerims">Array of lists of Vector2s.  Each list describes a perimeter, whether it be
        /// the complex polygon's outer perimeters or holes (the 0'th index is always the outer perimeter).</param>
        /// <returns>A list of lists of Vector2s.  Each list describes a rectangle in coordinates that follow the
        /// isometric axis (and need to be converted back to the cartesian axis elsewhere).</returns>
        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public static (List<Chord>, List<ChordlessPolygon>) DecomposeComplexPolygonToRectangles(this List<Vector2>[] allIsoPerims)
        {
            if (allIsoPerims is null) throw new ArgumentNullException(nameof(allIsoPerims));

            foreach (List<Vector2> perim in allIsoPerims)
            {
                if (!GeometryFuncs.IsPolygonCCW(perim.ToArray())) perim.Reverse();
            }
            (List<Chord> chords, List<ChordlessPolygon> chordlessPolygons) = _ComplexToChordlessPolygons(allIsoPerims);
            return (chords, chordlessPolygons);
        }

        /// <summary>
        /// Partitions a complex polygon (with chords and holes) into a group of chordless polygons.
        /// </summary>
        /// <param name="allIsoPerims">Array of lists of Vector2s, each describing a perimeter of a tile group.</param>
        /// <returns>A list of lists of Vector2s, each list describing a chordless polygon.</returns>
        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private static (List<Chord>, List<ChordlessPolygon>) _ComplexToChordlessPolygons(List<Vector2>[] allIsoPerims)
        {
            HashSet<ConcaveVertex> concaveVertices = ConcaveVertexFinder.GetConcaveVertices(allIsoPerims);
            List<Chord> chords = _FindChords(concaveVertices, allIsoPerims);
            Dictionary<BipartiteGraphNode, Chord> bipartiteNodeToChords = _ConvertChordsToNodes(chords);
            var bipartiteGraph = new BipartiteGraph(bipartiteNodeToChords.Keys);
            HashSet<BipartiteGraphNode> maxIndependentSet = bipartiteGraph.GetMaxIndependentSet();
            List<PolygonSplittingGraphNode> polygonSplittingNodes = _ConstructPolygonSplittingNodes(allIsoPerims, bipartiteNodeToChords, maxIndependentSet);
            var holes = new List<List<Vector2>>();
            for (int i = 1; i < allIsoPerims.Length; i++)
            {
                holes.Add(allIsoPerims[i]);
            }
            var polygonSplittingGraph = new PolygonSplittingGraph(polygonSplittingNodes, holes);
            List<ChordlessPolygon> minCycles = polygonSplittingGraph.GetMinCycles();
            return (chords, minCycles);
        }

        /// <summary>
        /// Given an irregular polygon described by <param>allIsoPerims</param>, find all of its chords and
        /// return a list containing them.
        /// </summary>
        /// <param name="concaveVertices">HashSet containing all concave vertices in polygon.</param>
        /// <param name="allIsoPerims">Array of lists of Vector2s, where each list describes a perim of a polygon, whether
        /// that perim be the polygon's outer perimeter or its hole perimeters.</param>
        /// <returns>A list of chords within <param>allIsoPerims</param>.</returns>
        private static List<Chord> _FindChords(HashSet<ConcaveVertex> concaveVertices, List<Vector2>[] allIsoPerims)
        {
            var chords = new List<Chord>();
            ConcaveVertex[] concaveVerticesList = concaveVertices.ToArray();
            for (int i = 0; i < concaveVerticesList.Length - 1; i++)
            {
                for (int j = i + 1; j < concaveVerticesList.Length; j++)
                {
                    Vector2 pointA = concaveVerticesList[i].vertex;
                    Vector2 pointB = concaveVerticesList[j].vertex;
                    if (!_IsChordValid(pointA, pointB, allIsoPerims, chords)) continue;
                    var chord = new Chord(pointA, pointB);
                    chords.Add(chord);
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
        /// 4 WHICH I FORGOT. The segment does not intersect any part of the perimeter
        /// 5 WHICH I ALSO FORGOT. The segment is actually within the polygon.
        /// </summary>
        /// <param name="pointA"></param>
        /// <param name="pointB"></param>
        /// <param name="allIsoPerims"></param>
        /// <param name="chords"></param>
        /// <returns></returns>
        private static bool _IsChordValid(Vector2 pointA, Vector2 pointB, List<Vector2>[] allIsoPerims,
                                          IEnumerable<Chord> chords)
        {
            if (chords.Any(chord => (chord.a == pointA && chord.b == pointB) || (chord.a == pointB && chord.b == pointA)))
            { //if not already in chords array
                return false;
            }

            if (pointA == pointB) return false; //if vertices are different
            if (pointA.x != pointB.x && pointA.y != pointB.y) return false; //if the segment is vertical or horizontal
            Vector2 midpoint = (pointB - pointA) / 2 + pointA;
            for (int i = 0; i < allIsoPerims.Length; i++)
            {
                List<Vector2> perims = allIsoPerims[i];
                if (i == 0)
                { //midpoint not in poly
                    if (!GeometryFuncs.IsPointInPoly(midpoint, perims.ToArray()))
                    {
                        return false;
                    }
                }
                else
                { //midpoint in hole
                    if (GeometryFuncs.IsPointInPoly(midpoint, perims.ToArray()))
                    {
                        return false;
                    }
                }
                for (int j = 0; j < perims.Count - 1; j++) //i < perims.Count - 1 because perims[0] = perims[last]
                { //if segment does not contain a part of the polygon's perimeter(s)
                    Vector2 perimVertexA = perims[j];
                    Vector2 perimVertexB = perims[j + 1];
                    if (GeometryFuncs.DoSegmentsOverlap(pointA, pointB, perimVertexA, perimVertexB))
                    { //segment confirmed to contain part of polygon's perimeter(s)
                        return false;
                    }
                    if (perimVertexA != pointA && perimVertexA != pointB && perimVertexB != pointA && perimVertexB != pointB)
                    {
                        if (GeometryFuncs.DoSegmentsIntersect(pointA, pointB, perimVertexA, perimVertexB))
                        { //segment intersects part of perimeter
                            return false;
                        }
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
        private static Dictionary<BipartiteGraphNode, Chord> _ConvertChordsToNodes(IReadOnlyList<Chord> chords)
        {
            var bipartiteNodeToChords = new Dictionary<BipartiteGraphNode, Chord>();
            for (int i = 0; i < chords.Count; i++)
            {
                Chord chord = chords[i];
                var connectedNodeIDs = new List<int>();
                for (int j = 0; j < chords.Count; j++)
                {
                    Chord comparisonChord = chords[j];
                    if (j == i || comparisonChord.direction == chord.direction) continue;
                    if (GeometryFuncs.DoSegmentsIntersect(chord.a, chord.b, comparisonChord.a, comparisonChord.b))
                    { //chord B is connected to chord A IFF they intersect, B != A, and they have different orientations
                        connectedNodeIDs.Add(j);
                    }
                }
                BipartiteGraphNode.BipartiteSide side = (chord.direction == Chord.Direction.Vertical) ?
                                                        BipartiteGraphNode.BipartiteSide.Left :
                                                        BipartiteGraphNode.BipartiteSide.Right;
                var bipartiteGraphNode = new BipartiteGraphNode(i, connectedNodeIDs, side);
                bipartiteNodeToChords.Add(bipartiteGraphNode, chord);
            }
            return bipartiteNodeToChords;
        }

        /// <summary>
        /// Given the polygon perimeters and its drawn chords, construct nodes out of each vertex and lists of each of their
        /// connected edges into a list of the class ChordSplittingGraphNode which will be used to split the polygon into
        /// chordless polygons.
        /// A special case exists where any vertex with four connections needs to be split into two vertices with two
        /// connections each, because this only happens when a hole shares a vertex with a concave vertex on the outer
        /// perimeter, and NOT splitting them would cause PolygonSplittingGraph to put them in the same ConnectedNodeGroup
        /// (thus losing a hole later down the line).
        /// </summary>
        /// <param name="allIsoPerims">Array of lists describing the polygon perimeters.</param>
        /// <param name="bipartiteNodeToChords">Map of all BipartiteNodes to Chords.</param>
        /// <param name="maxIndependentSet">MIS of BipartiteNodes which contains WHICH chords we should 'draw'.</param>
        /// <returns>A list of ChordSplittingGraphNodes that describe each vertex of the polygon (with chords included).</returns>
        private static List<PolygonSplittingGraphNode> _ConstructPolygonSplittingNodes(List<Vector2>[] allIsoPerims,
                                                                                       Dictionary<BipartiteGraphNode, Chord> bipartiteNodeToChords,
                                                                                       HashSet<BipartiteGraphNode> maxIndependentSet)
        {
            var polygonSplittingNodes = new List<PolygonSplittingGraphNode>();
            var vertexToID = new Dictionary<Vector2, int>();
            int nodeID = 0;
            foreach (List<Vector2> perim in allIsoPerims)
            {
                for (int i = 0; i < perim.Count - 1; i++)
                {
                    Vector2 vertex = perim[i];
                    if (vertexToID.ContainsKey(vertex)) continue;
                    vertexToID.Add(vertex, nodeID);
                    nodeID++;
                }
            }

            SortedList<int, List<int>> nodeConnectionsByID = _FindVertexConnections(allIsoPerims, bipartiteNodeToChords, maxIndependentSet, vertexToID);
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
        /// Special care is made to prevent vertices from having four connections, so vertices are divided into two vertices
        /// with two connections each.
        /// </summary>
        /// <param name="allIsoPerims">Array of lists describing the polygon perimeters.</param>
        /// <param name="bipartiteNodeToChords">Map of all BipartiteNodes to Chords.</param>
        /// <param name="maxIndependentSet">MIS of BipartiteNodes which contains WHICH chords we should 'draw'.</param>
        /// <param name="vertexToID">Dictionary mapping vertices to an ID.</param>
        /// <returns>Lists of connected IDs for every vertex.</returns>
        private static SortedList<int, List<int>> _FindVertexConnections(List<Vector2>[] allIsoPerims,
                                                                         Dictionary<BipartiteGraphNode, Chord> bipartiteNodeToChords,
                                                                         HashSet<BipartiteGraphNode> maxIndependentSet,
                                                                         Dictionary<Vector2, int> vertexToID)
        {
            var nodeConnectionsByID = new SortedList<int, List<int>>();
            foreach (List<Vector2> perim in allIsoPerims)
            {
                for (int i = 0; i < perim.Count - 1; i++)
                { //Add polygon edges first
                    var thisNodesConnections = new List<int>();
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
                    if (!nodeConnectionsByID.ContainsKey(vertexID)) nodeConnectionsByID.Add(vertexID, thisNodesConnections);
                    else nodeConnectionsByID[vertexID].AddRange(thisNodesConnections);
                }
            }

            foreach (BipartiteGraphNode bipartiteNode in maxIndependentSet)
            { //add chords to connected nodes
                Chord chord = bipartiteNodeToChords[bipartiteNode];
                int idA = vertexToID[chord.a];
                int idB = vertexToID[chord.b];
                nodeConnectionsByID[idA].Add(idB);
                nodeConnectionsByID[idB].Add(idA);
            }
            return nodeConnectionsByID;
        }
    }
}