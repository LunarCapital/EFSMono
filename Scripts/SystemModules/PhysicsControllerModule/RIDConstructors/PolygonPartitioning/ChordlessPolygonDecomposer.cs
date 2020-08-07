using System.Collections.Generic;
using System.Linq;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects;
using EFSMono.Scripts.SystemModules.PhysicsControllerModule.RIDConstructors.PolygonPartitioning.PolygonObjects;
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
            GD.PrintS("DECOMPOSING CHORDLESS POLYGON WITH PERIM: ");
            foreach (Vector2 vertex in polygon.outerPerim)
            {
                GD.PrintS("vertex: " + vertex);
            }
            foreach (List<Vector2> hole in polygon.holes)
            {
                foreach (Vector2 vertex in hole)
                {
                    GD.PrintS("vertex in hole: " + vertex);
                }
            }
            HashSet<ConcaveVertex> concaveVertices = _GetConcaveVertices(polygon);
            foreach (ConcaveVertex concaveVertex in concaveVertices)
            {
                GD.PrintS("Concave vertex: " + concaveVertex.vertex);
            }
            HashSet<ConcaveVertex> duplicateConcaveVertices = _GetDuplicateConcaveVertices(concaveVertices, polygon);
            foreach (ConcaveVertex duplicate in duplicateConcaveVertices)
            {
                concaveVertices.Remove(duplicate);
            }
            foreach (ConcaveVertex concaveVertex in concaveVertices)
            {
                GD.PrintS("Post duplicate removal concave vertex: " + concaveVertex.vertex);
            }
            
            List<PolyEdge> extensionSegments = _FindVertexExtensions(polygon, concaveVertices, chords);
            HashSet<Vector2> extensionVertices = _GetNewVertices(polygon, extensionSegments);
            GD.PrintS("New xtension vertices:");
            foreach (Vector2 vertex in extensionVertices)
            {
                GD.PrintS(vertex);
            }
            
            (List<Vector2> newPerim, List<List<Vector2>> newHoles) =  _InsertNewVerticesIntoPerims(polygon, extensionVertices);
            List<PolygonSplittingGraphNode> nodes = _CreateNewNodes(newPerim, newHoles, extensionSegments);
            var polygonSplittingGraph = new PolygonSplittingGraph(nodes, newHoles);

            foreach (PolygonSplittingGraphNode node in nodes)
            {
                GD.PrintS("chordless node with id: " + node.id + " at coord: " + node.x + ", " + node.y + " has neighbours: ");
                foreach (int id in node.connectedNodeIDs)
                {
                    GD.PrintS(id);
                }
            }
            
            List<ChordlessPolygon> newRectangles = polygonSplittingGraph.GetMinCycles();
            rectangles.AddRange(newRectangles.Select(rectangle => rectangle.outerPerim.ToList()));
        }
        return rectangles;
    }

    /// <summary>
    /// Checks polygon vertices (of both its outer perimeters and holes) and returns a list of the concave ones.
    /// </summary>
    /// <param name="polygon"></param>
    /// <returns>List of concave vertices.</returns>
    private static HashSet<ConcaveVertex> _GetConcaveVertices(ChordlessPolygon polygon)
    {
        var concaveVertices = new HashSet<ConcaveVertex>();
        concaveVertices.UnionWith(_TracePerimeterForConcaveVertices(polygon.outerPerim));

        foreach (List<Vector2> hole in polygon.holes)
        {
            concaveVertices.UnionWith(_TracePerimeterForConcaveVertices(hole, true));
        }
        return concaveVertices;
    }

    /// <summary>
    /// Iterates through the input <param>perimeter</param> from the vertex with the min X and min Y coordinates (which
    /// is ALWAYS convex) in CCW order.  Three vertices are needed to check whether the middle vertex is concave or not,
    /// AKA some sequence of vertices P, Q, and R can be used to determine Q given that the vertices are NOT collinear.
    /// Q is concave IF the midpoint between P and R is WITHIN the polygon, AKA:
    ///     * If the perimeter being checked is not a hole (AKA it is the polygon's outer perim), then P->R's midpoint
    ///       must be inside the polygon for Q to be concave.
    ///     * If the perimeter being checked IS a hole, then P->R's midpoint must NOT be inside the hole for Q to be
    ///       concave.
    /// </summary>
    /// <param name="perimeter">Perimeter of either polygon or its holes.</param>
    /// <returns>List of concave vertices.</returns>
    private static HashSet<ConcaveVertex> _TracePerimeterForConcaveVertices(IReadOnlyList<Vector2> perimeter, bool hole = false)
    {
        var concaveVertices = new HashSet<ConcaveVertex>();
        int startID = GeometryFuncs.GetMinXMinYCoord(perimeter);
        for (int i = 0; i < perimeter.Count - 1; i++) //Count - 1 because of closed loop convention
        { //as a side note, perim is always ordered CCW thanks to EdgeCollection's simplification algorithm.
            int thisIndex = (i + startID);
            Vector2 thisVertex = perimeter[thisIndex % (perimeter.Count - 1)];
            Vector2 prevVertex = perimeter[(thisIndex - 1 + perimeter.Count - 1) % (perimeter.Count - 1)];
            Vector2 nextVertex = perimeter[(thisIndex + 1) % (perimeter.Count - 1)];
            float angle = Mathf.Rad2Deg(Mathf.Atan2(thisVertex.y - prevVertex.y, thisVertex.x - prevVertex.x) - 
                                        Mathf.Atan2(nextVertex.y - thisVertex.y, nextVertex.x - thisVertex.x));
            GD.PrintS("prev to thsi to next: " + prevVertex + ", " + thisVertex + ", " + nextVertex + ", angle: " + angle);
            if (angle < 0) angle += 360;
            if (angle == 90 && hole)
            { //hole with 90 degree angle would be 270 on the other side .'. concave
                concaveVertices.Add(new ConcaveVertex(thisVertex, prevVertex, nextVertex));
            }
            else if (angle == 270 && !hole)
            {
                concaveVertices.Add(new ConcaveVertex(thisVertex, prevVertex, nextVertex));
            }
        }
        return concaveVertices;
    }

    
    /// <summary>
    /// Checks how many times a concave vertex appears in an input polygon's perimeters and adds them to a list of
    /// duplicates IFF the vertex appears more than once.
    /// </summary>
    /// <param name="polygon"></param>
    /// <returns>A list of concave vertices that appear in the input polygon's perimeters more than once.</returns>
    private static HashSet<ConcaveVertex> _GetDuplicateConcaveVertices(HashSet<ConcaveVertex> concaveVertices, ChordlessPolygon polygon)
    {
        var duplicateVertices = new HashSet<ConcaveVertex>();
        foreach (ConcaveVertex concaveVertex in concaveVertices)
        {
            int appearanceCount = 0;
            foreach (Vector2 perimVertex in polygon.outerPerim)
            {
                if (perimVertex == concaveVertex.vertex)
                {
                    appearanceCount++;
                    break;
                }
            }
            foreach (List<Vector2> hole in polygon.holes)
            {
                foreach (Vector2 holeVertex in hole)
                {
                    if (holeVertex == concaveVertex.vertex)
                    {
                        appearanceCount++;
                        break;
                    }
                }
            }
            if (appearanceCount > 1) duplicateVertices.Add(concaveVertex);
        }
        return duplicateVertices;
    }
    
    /// <summary>
    /// Gets chordless polygon extensions as a list of PolyEdges.  Always fills in bridges first.
    /// </summary>
    /// <param name="polygon"></param>
    /// <param name="concaveVertices">Vertices in polygon that need to be extended.</param>
    /// <returns>List of PolyEdges representing extensions.</returns>
    private static List<PolyEdge> _FindVertexExtensions(ChordlessPolygon polygon, HashSet<ConcaveVertex> concaveVertices, List<Chord> chords)
    {
        (List<PolyEdge> extensions, HashSet<Vector2> alreadyExtended) = _AddBridgesToExtensions(polygon);
        foreach (ConcaveVertex concaveVertex in concaveVertices)
        {
            if (alreadyExtended.Contains(concaveVertex.vertex)) continue;
            GD.PrintS("Vertex: " + concaveVertex.vertex + " can extend hori: " + concaveVertex.GetHorizontalFreeDirection() + " or vert: " + concaveVertex.GetVerticalFreeDirection());
            Vector2 freeDir = concaveVertex.GetHorizontalFreeDirection();
            if (_IsDirectionChordFree(concaveVertex.vertex, freeDir, concaveVertices, chords))
            { //ensure that if we were to extend in this direction we would not create a chord
                GD.PrintS("horizontal free");
                if (!_IsDirectionChordFree(concaveVertex.vertex, concaveVertex.GetVerticalFreeDirection(), concaveVertices, chords))
                {
                    GD.PrintS("But vert not free");
                }
            }
            else
            {
                GD.PrintS("Horizontal NOT FREE");
                freeDir = concaveVertex.GetVerticalFreeDirection();
            }
            Vector2 overextension = _GetOverextendedCoord(polygon, concaveVertex.vertex, freeDir);
            GD.PrintS("Vertex overextends to: " + overextension);
            Vector2 cutExtension = _GetCutExtension(polygon, extensions, concaveVertex.vertex, overextension);
            GD.PrintS("Vertex extension is cut down to: " + cutExtension);
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
            GD.PrintS("horizontal cut: " + cutExtension + " using segment: " + segmentA + ", " + segmentB);
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
                GD.PrintS("confimred: Between: " + thisVertex + " and " + nextVertex + " is: " + vertex);
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