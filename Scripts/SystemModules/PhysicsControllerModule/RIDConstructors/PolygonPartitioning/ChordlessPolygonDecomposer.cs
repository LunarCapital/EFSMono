using System.Collections.Generic;
using System.Linq;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects;
using Godot;

namespace EFSMono.Scripts.SystemModules.PhysicsControllerModule.RIDConstructors.PolygonPartitioning
{
/// <summary>
/// A class intended to decompose chordless polygons (represented by the ChordlessPolygon class) into rectangles.
/// Can be done by extending concave vertices in any direction, except for the cases where the ChordlessPolygon has a
/// bridge (in which case the bridge MUST be the direction that the vertex is extended in) OR the extension would form
/// a chord (in which case this class should extend in a different direction).
/// </summary>
public static class ChordlessPolygonDecomposer
{

    /// <summary>
    /// Splits a chordless polygon into rectangles by extending its concave vertices according to the following criteria:
    ///     1. If the vertex has a bridge, extend in that direction.
    ///     2. If possible to extend in a direction that forms a chord, do NOT do it.
    ///     3. If neither of the above, either direction works.
    /// This method then builds PolygonSplittingGraphNodes to make a PolygonSplittingGraph and then runs the GetMinCycles
    /// func in order to get rectangles.
    /// </summary>
    /// <param name="chordlessPolygons">A list of chordless polygons.</param>
    /// <returns>A list of lists of four vector2s, AKA a list of lists of rectangles.</returns>
    public static List<List<Vector2>> DecomposeChordlessPolygonToRectangles(this List<ChordlessPolygon> chordlessPolygons,
                                                                            List<Chord> chords)
    {
        var rectangles = new List<List<Vector2>>(); 
        foreach (ChordlessPolygon polygon in chordlessPolygons)
        {
            HashSet<ConcaveVertex> concaveVertices = ConcaveVertexFinder.GetConcaveVertices(polygon);
            List<PolyEdge> extensionSegments = _FindVertexExtensions(polygon, concaveVertices, chords);
            HashSet<Vector2> extensionVertices = _GetNewVertices(polygon, extensionSegments);
            (List<Vector2> newPerim, List<List<Vector2>> newHoles) =  _InsertNewVerticesIntoPerims(polygon, extensionVertices);
            List<PolygonSplittingGraphNode> nodes = _CreateNewNodes(newPerim, newHoles, extensionSegments);
            var polygonSplittingGraph = new PolygonSplittingGraph(nodes, newHoles);
            List<ChordlessPolygon> newRectangles = polygonSplittingGraph.GetMinCycles();
            rectangles.AddRange(newRectangles.Select(rectangle => rectangle.outerPerim.ToList()));
        }
        return rectangles;
    }

    /// <summary>
    /// Gets chordless polygon extensions as a list of PolyEdges.  Always fills in bridges first.
    /// </summary>
    /// <param name="polygon"></param>
    /// <param name="concaveVertices">Vertices in polygon that need to be extended.</param>
    /// <param name="chords"></param>
    /// <returns>List of PolyEdges representing extensions.</returns>
    private static List<PolyEdge> _FindVertexExtensions(ChordlessPolygon polygon, HashSet<ConcaveVertex> concaveVertices, List<Chord> chords)
    {
        (List<PolyEdge> extensions, HashSet<Vector2> alreadyExtended) = _AddBridgesToExtensions(polygon);
        foreach (ConcaveVertex concaveVertex in concaveVertices)
        {
            if (alreadyExtended.Contains(concaveVertex.vertex)) continue;
            Vector2 freeDir = concaveVertex.GetHorizontalFreeDirection();
            if (_IsDirectionChordFree(concaveVertex.vertex, freeDir, concaveVertices, chords))
            { //ensure that if we were to extend in this direction we would not create a chord
            }
            else
            {
                freeDir = concaveVertex.GetVerticalFreeDirection();
            }
            Vector2 overextension = _GetOverextendedCoord(polygon, concaveVertex.vertex, freeDir);
            Vector2 cutExtension = _GetCutExtension(polygon, extensions, concaveVertex.vertex, overextension);
            extensions.Add(new PolyEdge(concaveVertex.vertex, cutExtension));
        }
        return extensions;
    }

    private static (List<PolyEdge>, HashSet<Vector2>) _AddBridgesToExtensions(ChordlessPolygon polygon)
    {
        var extensions = new List<PolyEdge>();
        var alreadyExtended = new HashSet<Vector2>();
        foreach (Vector2 bridgeA in polygon.bridges.Keys)
        {
            foreach (Vector2 bridgeB in polygon.bridges[bridgeA])
            {
                var bridgeExtension = new PolyEdge(bridgeA, bridgeB);
                if (!extensions.Contains(bridgeExtension) && !extensions.Contains(bridgeExtension.GetReverseEdge()))
                {
                    extensions.Add(bridgeExtension);
                    alreadyExtended.Add(bridgeA);
                    alreadyExtended.Add(bridgeB);
                }
            }
        }
        return (extensions, alreadyExtended);
    }

    /// <summary>
    /// Checks if there are any other concave vertices in some direction from the origin.
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="direction"></param>
    /// <param name="concaveVertices"></param>
    /// <param name="chords"></param>
    /// <returns>Returns true if from the origin in some input direction there is a different concave vertex.</returns>
    private static bool _IsDirectionChordFree(Vector2 origin, Vector2 direction, HashSet<ConcaveVertex> concaveVertices, List<Chord> chords)
    {
        foreach (ConcaveVertex concaveVertex in concaveVertices)
        {
            if ((concaveVertex.vertex - origin).Normalized() == direction)
            { 
                if (chords.Exists(c => (c.a == origin && c.b == concaveVertex.vertex) ||
                                             (c.b == origin && c.a == concaveVertex.vertex)))
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Given an origin and a direction to extend towards, returns the coordinate that is 'far' enough that it goes
    /// beyond the bounds of the polygon.
    /// </summary>
    /// <param name="polygon">Polygon owning the vertex being extended.</param>
    /// <param name="origin">Concave vertex being extended.</param>
    /// <param name="freeDir">Direction that the extension is in.</param>
    /// <returns>Vector2 representing a coordinate beyond the bounds of the polygon's perimeter.</returns>
    private static Vector2 _GetOverextendedCoord(ChordlessPolygon polygon, Vector2 origin, Vector2 freeDir)
    {
        float furthest = (freeDir.x == 0) ? origin.y : origin.x;
        for (int i = 0; i < polygon.outerPerim.Length - 1; i++)
        {
            Vector2 perimVertex = polygon.outerPerim[i];
            if (freeDir.x == 0)
            { //vertical direction
                if (freeDir.y > 0 && perimVertex.y > origin.y)
                { //down
                    if (perimVertex.y > furthest) furthest = perimVertex.y;
                }
                else if (freeDir.y < 0 && perimVertex.y < origin.y)
                { //up
                    if (perimVertex.y < furthest) furthest = perimVertex.y;
                }
            }
            else if (freeDir.y == 0)
            { //horizontal direction
                if (freeDir.x > 0 && perimVertex.x > origin.x)
                { //right
                    if (perimVertex.x > furthest) furthest = perimVertex.x;
                }
                else if (freeDir.x < 0 && perimVertex.x < origin.x)
                { //left
                    if (perimVertex.x < furthest) furthest = perimVertex.x;
                }
            }
        }
        Vector2 overextension = (freeDir.x == 0) ? new Vector2(origin.x, furthest) : new Vector2(furthest, origin.y);
        return overextension + freeDir;
    }

    /// <summary>
    /// Cut down the input overextension coordinate to the closest edge.  Checks edges from:
    ///     1. The polygon's perimeter
    ///     2. The polygon's holes perimeters'
    ///     3. Extensions that already exist
    /// </summary>
    /// <param name="polygon">Input polygon.</param>
    /// <param name="extensions">Extension edges that already exist.</param>
    /// <param name="origin">Concave vertex being extended.</param>
    /// <param name="overextension">Vector2 representing the other side of the extension that is currently too far
    /// and needs to be cut down.</param>
    /// <returns>A vector2 representing the other side of where the extension should be, AKA the first (closest) edge it
    /// hits.</returns>
    private static Vector2 _GetCutExtension(ChordlessPolygon polygon, List<PolyEdge> extensions,
                                        Vector2 origin, Vector2 overextension)
    {
        Vector2 cutExtension = overextension;
        for (int i = 0; i < polygon.outerPerim.Length - 1; i++)
        {
            Vector2 perimCoordA = polygon.outerPerim[i];
            Vector2 perimCoordB = polygon.outerPerim[i + 1];
            if (GeometryFuncs.AreSegmentsParallel(origin, overextension, perimCoordA, perimCoordB) ||
                                                        perimCoordA == origin || perimCoordB == origin) continue;
            Vector2 potentialCutExtension = _CutExtensionWithSegment(origin, overextension, perimCoordA, perimCoordB);
            if (origin.DistanceSquaredTo(potentialCutExtension) < origin.DistanceSquaredTo(cutExtension))
            {
                cutExtension = potentialCutExtension;
            }
        }

        foreach (List<Vector2> hole in polygon.holes)
        {
            for (int i = 0; i < hole.Count - 1; i++)
            {
                Vector2 perimCoordA = hole[i];
                Vector2 perimCoordB = hole[i + 1];
                if (GeometryFuncs.AreSegmentsParallel(origin, overextension, perimCoordA, perimCoordB) ||
                                                            perimCoordA == origin || perimCoordB == origin) continue;
                Vector2 potentialCutExtension = _CutExtensionWithSegment(origin, overextension, perimCoordA, perimCoordB);
                if (origin.DistanceSquaredTo(potentialCutExtension) < origin.DistanceSquaredTo(cutExtension))
                {
                    cutExtension = potentialCutExtension;
                }
            }
        }

        foreach (PolyEdge edge in extensions)
        {
            Vector2 extCoordA = edge.a;
            Vector2 extCoordB = edge.b;
            if (GeometryFuncs.AreSegmentsParallel(origin, overextension, extCoordA, extCoordB) ||
                                                        extCoordA == origin || extCoordB == origin) continue;
            Vector2 potentialCutExtension = _CutExtensionWithSegment(origin, overextension, extCoordA, extCoordB);
            if (origin.DistanceSquaredTo(potentialCutExtension) < origin.DistanceSquaredTo(cutExtension))
            {
                cutExtension = potentialCutExtension;
            }
        }
        return cutExtension;
    }

    /// <summary>
    /// Given some segment represented by <param>segmentA</param> and <param>segmentB</param> that is not parallel to
    /// the segment represented by <param>origin</param> and <param>overextension</param> and if necessary, cuts down
    /// the overextension to the segment IFF the segment cuts origin->overextension.
    /// If the two input segments do not intersect just returns overextension.
    /// </summary>
    /// <param name="origin">Concave vertex being extended.</param>
    /// <param name="overextension">Extension that should reach outside the polygon.</param>
    /// <param name="segmentA"></param>
    /// <param name="segmentB">Segment that is being used to cut the overextension.</param>
    /// <returns>A coordinate between origin and overextension closer to the origin IFF segment cuts origin->overextension,
    /// or overextension if the segment does not intersect origin->overextension.</returns>
    private static Vector2 _CutExtensionWithSegment(Vector2 origin, Vector2 overextension, Vector2 segmentA, Vector2 segmentB)
    {
        if (!GeometryFuncs.DoSegmentsIntersect(origin, overextension, segmentA, segmentB)) return overextension;

        Vector2 cutExtension = overextension;
        if (origin.x == overextension.x)
        { //VERTICAL
            cutExtension.y = segmentA.y;
        }
        else if (origin.y == overextension.y)
        { //HORIZONTAL
            cutExtension.x = segmentA.x;
        }
        
        return cutExtension;
    }

    /// <summary>
    /// Searches through the extension segments and makes a list of new vertices that did not exist previously.
    /// </summary>
    /// <param name="polygon">Polygon that new vertices have been found for.</param>
    /// <param name="extensionSegments">List of extension segments.</param>
    /// <returns>A list of new vertices.</returns>
    private static HashSet<Vector2> _GetNewVertices(ChordlessPolygon polygon, List<PolyEdge> extensionSegments)
    {
        var newVertices = new HashSet<Vector2>();
        var existingVertices = new HashSet<Vector2>();
        foreach (Vector2 vertex in polygon.outerPerim)
        {
            existingVertices.Add(vertex);
        }
        foreach (List<Vector2> hole in polygon.holes)
        {
            foreach (Vector2 vertex in hole)
            {
                existingVertices.Add(vertex);
            }
        }
        foreach (PolyEdge edge in extensionSegments)
        {
            if (!existingVertices.Contains(edge.a)) newVertices.Add(edge.a);
            if (!existingVertices.Contains(edge.b)) newVertices.Add(edge.b);
        }
        return newVertices;
    }

    /// <summary>
    /// Grabs the new vertices in <param>extensionVertices</param> and inserts them into <param>polygon</param>'s existing
    /// perimeters where they fit, and returns two new Lists (representing outer perim and holes respectively) that
    /// are the same as the polygon's current perimeters but with the new vertices inserted.
    /// </summary>
    /// <param name="polygon">Polygon that new vertices are being inserted into.</param>
    /// <param name="extensionVertices">New vertices created by extending the polygon's concave vertices to the closest
    /// edge.</param>
    /// <returns>Two lists, representing the polygon's outer perim and hole perims respectively, but with the new
    /// vertices inserted.</returns>
    private static (List<Vector2>, List<List<Vector2>>) _InsertNewVerticesIntoPerims(ChordlessPolygon polygon,
                                                                                     HashSet<Vector2> extensionVertices)
    {
        var outerPerimWithNewVertices = new List<Vector2>();
        var holesWithNewVertices = new List<List<Vector2>>();
        for (int i = 0; i < polygon.outerPerim.Length - 1; i++)
        {
            Vector2 thisVertex = polygon.outerPerim[i];
            Vector2 nextVertex = polygon.outerPerim[i + 1];
            List<Vector2> verticesInBetween = _GetVerticesInBetween(thisVertex, nextVertex, extensionVertices);
            outerPerimWithNewVertices.Add(thisVertex);
            IOrderedEnumerable<Vector2> orderedNewVertices = verticesInBetween.OrderBy(x => thisVertex.DistanceSquaredTo(x));
            outerPerimWithNewVertices.AddRange(orderedNewVertices);
        }
        outerPerimWithNewVertices.Add(polygon.outerPerim[polygon.outerPerim.Length - 1]);

        foreach (List<Vector2> hole in polygon.holes)
        {
            var holePerimWithNewVertices = new List<Vector2>();
            for (int i = 0; i < hole.Count - 1; i++)
            {
                Vector2 thisVertex = hole[i];
                Vector2 nextVertex = hole[i + 1];
                List<Vector2> verticesInBetween = _GetVerticesInBetween(thisVertex, nextVertex, extensionVertices);
                holePerimWithNewVertices.Add(thisVertex);
                IOrderedEnumerable<Vector2> orderedNewVertices = verticesInBetween.OrderBy(x => thisVertex.DistanceSquaredTo(x));
                holePerimWithNewVertices.AddRange(orderedNewVertices);
            }
            holePerimWithNewVertices.Add(hole[hole.Count - 1]);
            holesWithNewVertices.Add(holePerimWithNewVertices);
        }
        return (outerPerimWithNewVertices, holesWithNewVertices);
    }

    /// <summary>
    /// Searches for vertices in <param>extensionVertices</param> and returns a HashSet containing all of them that are
    /// between <param>thisVertex</param> and <param>nextVertex</param>.
    /// </summary>
    /// <param name="thisVertex"></param>
    /// <param name="nextVertex"></param>
    /// <param name="extensionVertices"></param>
    /// <returns>HashSet containing all vertices in <param>extensionVertices</param> between <param>thisVertex</param>
    /// and <param>nextVertex</param>.</returns>
    private static List<Vector2> _GetVerticesInBetween(Vector2 thisVertex, Vector2 nextVertex,
                                                          HashSet<Vector2> extensionVertices)
    {
        var verticesInBetween = new List<Vector2>();
        foreach (Vector2 vertex in extensionVertices)
        {
            if (vertex == thisVertex || vertex == nextVertex) continue;
            if (!GeometryFuncs.AreSegmentsCollinear(thisVertex, vertex, vertex, nextVertex)) continue;
            if ((thisVertex.x - vertex.x) * (nextVertex.x - vertex.x) <= 0 &&
                (thisVertex.y - vertex.y) * (nextVertex.y - vertex.y) <= 0)
            {
                verticesInBetween.Add(vertex);
            }
        }
        return verticesInBetween;
    }

    /// <summary>
    /// Creates a PolygonSplittingGraphNode for each vertex of the polygon, including new vertices formed by
    /// <param>extensionSegments</param>.  
    /// </summary>
    /// <param name="newPerim">Perimeter of polygon with inserted extended vertices.</param>
    /// <param name="newHoles">Perimeters of polygon holes with inserted extended vertices.</param>
    /// <param name="extensionSegments">Segments extended off from concave vertices to split the polygon into rectangles.</param>
    /// <returns>A list of PolygonSplittingGraphNode, each representing a polygon vertex.</returns>
    private static List<PolygonSplittingGraphNode> _CreateNewNodes(List<Vector2> newPerim, List<List<Vector2>> newHoles,
                                                                   List<PolyEdge> extensionSegments)
    {
        (Dictionary<Vector2, int> vertexToID, Dictionary<int, Vector2> idToVertex) =
            _InitialiseVectorBiDicts(newPerim, newHoles);
        var connections = new Dictionary<int, HashSet<int>>();
        foreach (int id in idToVertex.Keys)
        {
            connections[id] = new HashSet<int>();
        }
        connections = _AddPerimConnections(connections, newPerim, newHoles, vertexToID);
        connections = _AddExtensionConnections(connections, extensionSegments, vertexToID);

        var graphNodes = new List<PolygonSplittingGraphNode>();
        foreach (int id in connections.Keys)
        {
            Vector2 vertex = idToVertex[id];
            graphNodes.Add(new PolygonSplittingGraphNode(id, connections[id].ToList(), vertex.x, vertex.y));
        }
        return graphNodes;
    }

    /// <summary>
    /// Initialises two dictionaries that map vertices to an ID, and IDs to each vertex.
    /// </summary>
    /// <param name="newPerim">Perimeter of polygon with inserted extended vertices.</param>
    /// <param name="newHoles">Perimeters of polygon holes with inserted extended vertices.</param>
    /// <returns></returns>
    private static (Dictionary<Vector2, int>, Dictionary<int, Vector2>) _InitialiseVectorBiDicts(List<Vector2> newPerim,
                                                                                                 List<List<Vector2>> newHoles)
    {
        var vertexToID = new Dictionary<Vector2, int>();
        var idToVertex = new Dictionary<int, Vector2>();

        int id = 0;
        foreach (Vector2 vertex in newPerim)
        {
            if (vertexToID.ContainsKey(vertex)) continue;
            vertexToID.Add(vertex, id);
            idToVertex.Add(id, vertex);
            id++;
        }
        foreach (List<Vector2> hole in newHoles)
        {
            foreach (Vector2 vertex in hole)
            {
                if (vertexToID.ContainsKey(vertex)) continue;
                vertexToID.Add(vertex, id);
                idToVertex.Add(id, vertex);
                id++;
            }
        }
        return (vertexToID, idToVertex);
    }

    /// <summary>
    /// Iterates through the input <param>polygon</param>'s perimeters (including hole perimeters) and stores adjacent
    /// vertices as node connections.  IF there is an extension vertex in between two perimeter vertices, then two
    /// connections are made (from each perim vertex to the extension vertex) instead of the two perim vertices being
    /// connected to each other.
    /// </summary>
    /// <param name="connections">Empty dictionary of connections, but with all its ID lists initialised.</param>
    /// <param name="newPerim">Perimeter of polygon with inserted extended vertices.</param>
    /// <param name="newHoles">Perimeters of polygon holes with inserted extended vertices.</param>
    /// <param name="vertexToID">Map of vertex to an ID.</param>
    /// <returns>Dictionary mapping a vertex ID to the ID of all its connected vertices.</returns>
    private static Dictionary<int, HashSet<int>> _AddPerimConnections(Dictionary<int, HashSet<int>> connections,
                                                                      List<Vector2> newPerim,
                                                                      List<List<Vector2>> newHoles,
                                                                      Dictionary<Vector2, int> vertexToID)
    {
        var updatedConnections = new Dictionary<int,HashSet<int>>(connections);
        for (int i = 0; i < newPerim.Count - 1; i++)
        {
            Vector2 thisVertex = newPerim[i];
            Vector2 nextVertex = newPerim[i + 1];
            updatedConnections[vertexToID[thisVertex]].Add(vertexToID[nextVertex]); 
            updatedConnections[vertexToID[nextVertex]].Add(vertexToID[thisVertex]);
        }
        foreach (List<Vector2> hole in newHoles)
        {
            for (int i = 0; i < hole.Count - 1; i++)
            {
                Vector2 thisVertex = hole[i];
                Vector2 nextVertex = hole[i + 1];
                updatedConnections[vertexToID[thisVertex]].Add(vertexToID[nextVertex]); 
                updatedConnections[vertexToID[nextVertex]].Add(vertexToID[thisVertex]);
            }
        }
        return updatedConnections;
    }

    /// <summary>
    /// Adds all extension segments as node-to-node connections.
    /// </summary>
    /// <param name="connections">Dictionary of connections, but only inclusive of perimeter connections (until this method
    /// is finished).</param>
    /// <param name="extensionSegments">All extended concave vertices.</param>
    /// <param name="vertexToID">Map of vertex to an ID.</param>
    /// <returns>Dictionary mapping a vertex ID to the ID of all its connected vertices.</returns>
    private static Dictionary<int, HashSet<int>> _AddExtensionConnections(Dictionary<int, HashSet<int>> connections, 
                                                                          List<PolyEdge> extensionSegments,
                                                                          Dictionary<Vector2, int> vertexToID)
    {
        var updatedConnections = new Dictionary<int, HashSet<int>>(connections);
        foreach (PolyEdge edge in extensionSegments)
        {
            updatedConnections[vertexToID[edge.a]].Add(vertexToID[edge.b]);
            updatedConnections[vertexToID[edge.b]].Add(vertexToID[edge.a]);
        }
        return updatedConnections;
    }
}
}