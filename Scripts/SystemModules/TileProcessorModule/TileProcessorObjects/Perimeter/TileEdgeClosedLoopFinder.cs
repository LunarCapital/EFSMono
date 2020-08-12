using System.Collections.Generic;
using System.Linq;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.DataStructures.Geometry;
using EFSMono.Scripts.DataStructures.Graphs.PolygonSplittingGraphObjects;
using Godot;

namespace EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter
{
/// <summary>
/// Class that extends an EdgeCollection of TileEdges to find closed loops (outer perim and holes).
/// </summary>
public static class TileEdgeClosedLoopFinder
{
    /// <summary>
    /// Attempts to get a collection of edges in CCW order that form the BIGGEST closed loop possible. Remaining edges
    /// are left behind.
    /// </summary>
    /// <param name="edgeColl"></param>
    /// <returns>EdgeCollection of edges in CCW order that form the biggest closed loop possible out of the edges
    /// currently in this collection.</returns>
    public static EdgeCollection<TileEdge> GetOuterClosedLoop(this EdgeCollection<TileEdge> edgeColl)
    {
        Dictionary<PolyEdge, TileEdge> polyToTileMap = _InitTilePolyBiDict(edgeColl);
        SortedList<int, PolygonSplittingGraphNode> polygonNodes = _CreatePolygonGraphSplittingNodes(polyToTileMap);
        var group = new ConnectedNodeGroup(0, polygonNodes);
        List<PolygonSplittingGraphNode> outerNodes = group.outerPerimNodes;
        List<TileEdge> outerEdges = _ConvertPolygonNodesToPolyEdges(outerNodes, polyToTileMap);
        return new EdgeCollection<TileEdge>(outerEdges);
    }

    /// <summary>
    /// Attempts to get a collection of edges in CCW order that is a small cycle.  Uses PolygonSplittingGraphNode to
    /// extract small cycles. 
    /// </summary>
    /// <param name="edgeColl"></param>
    /// <returns>CCW collection of edges that forms a small loop(s).</returns>
    public static List<EdgeCollection<TileEdge>> GetSmallClosedLoops(this EdgeCollection<TileEdge> edgeColl)
    {
        EdgeCollection<TileEdge> connectedColl = edgeColl.GetOrderedCollection();
        Dictionary<PolyEdge, TileEdge> polyToTileMap = _InitTilePolyBiDict(connectedColl);
        SortedList<int, PolygonSplittingGraphNode> polygonNodes = _CreatePolygonGraphSplittingNodes(polyToTileMap);
        var graph = new PolygonSplittingGraph(polygonNodes.Values.ToList());
        List<ChordlessPolygon> smallLoops = graph.GetMinCycles();

        var allTileEdgeLoops = new List<EdgeCollection<TileEdge>>();
        foreach (ChordlessPolygon poly in smallLoops)
        {
            List<TileEdge> smallLoopsEdges = _ConvertChordlessPolygonToPolyEdges(poly.outerPerimUnsimplified, polyToTileMap);
            allTileEdgeLoops.Add(new EdgeCollection<TileEdge>(smallLoopsEdges));
        }
        return allTileEdgeLoops;
    }
    
    /// <summary>
    /// Creates PolyEdges from all TileEdges in the isometric axis (instead of the cartesian axis) and maps them to
    /// each other.
    /// </summary>
    /// <param name="edgeColl"></param>
    /// <returns>Two Dictionaries that map TileEdges to their respective PolyEdges and vice versa.</returns>
    private static Dictionary<PolyEdge, TileEdge> _InitTilePolyBiDict(EdgeCollection<TileEdge> edgeColl)
    {
        var polyToTileMap = new Dictionary<PolyEdge, TileEdge>();
        foreach (TileEdge tileEdge in edgeColl)
        {
            var polyEdge = new PolyEdge(AxisFuncs.CoordToIsoAxis(tileEdge.a), AxisFuncs.CoordToIsoAxis(tileEdge.b));
            polyToTileMap[polyEdge] = tileEdge;
        }
        return polyToTileMap;
    }

    /// <summary>
    /// Creates PolygonSplittingGraphNodes for each vertex using PolyEdges as connections.
    /// </summary>
    /// <param name="polyToTileMap">A Dictionary mapping PolyEdges to their respective TileEdges (although in this
    /// method we just want the PolyEdges).</param>
    /// <returns>List of PolygonSplittingGraphNodes that represent the vertices/edges formed by the input PolyEdges</returns>
    private static SortedList<int, PolygonSplittingGraphNode> _CreatePolygonGraphSplittingNodes(Dictionary<PolyEdge, TileEdge> polyToTileMap)
    {
        (Dictionary<Vector2, int> vertexToID, Dictionary<int, Vector2> idToVertex) = _InitialiseVertexIDBiDicts(polyToTileMap);
        var nodeConnections = new Dictionary<int, HashSet<int>>();
        foreach (int id in idToVertex.Keys)
        {
            nodeConnections[id] = new HashSet<int>();
        }
        foreach (PolyEdge polyEdge in polyToTileMap.Keys)
        {
            int aID = vertexToID[polyEdge.a];
            int bID = vertexToID[polyEdge.b];
            nodeConnections[aID].Add(bID);
            nodeConnections[bID].Add(aID);
        }
        var polygonNodes = new SortedList<int, PolygonSplittingGraphNode>();
        foreach (int id in idToVertex.Keys)
        {
            var node = new PolygonSplittingGraphNode(id, nodeConnections[id].ToList(), idToVertex[id].x, idToVertex[id].y);
            polygonNodes.Add(id, node);
        }

        return polygonNodes;
    }
    
    /// <summary>
    /// Initialise two dictionaries that map vertices to IDs and vice versa.
    /// </summary>
    /// <param name="polyToTileMap">A Dictionary mapping PolyEdges to their respective TileEdges.</param>
    /// <returns></returns>
    private static (Dictionary<Vector2, int>, Dictionary<int, Vector2>) _InitialiseVertexIDBiDicts(Dictionary<PolyEdge, TileEdge> polyToTileMap)
    {
        var vertexToID = new Dictionary<Vector2, int>();
        var idToVertex = new Dictionary<int, Vector2>();
        int id = 0;
        foreach (PolyEdge polyEdge in polyToTileMap.Keys)
        {
            Vector2 a = polyEdge.a;
            Vector2 b = polyEdge.b;
            if (!vertexToID.ContainsKey(a))
            {
                vertexToID[a] = id;
                idToVertex[id] = a;
                id++;
            }

            if (!vertexToID.ContainsKey(b))
            {
                vertexToID[b] = id;
                idToVertex[id] = b;
                id++;
            }
        }
        return (vertexToID, idToVertex);
    }

    /// <summary>
    /// Converts an outer perimeter represented by PolygonSplittingGraphNodes to a list of PolyEdges in the same order.
    /// </summary>
    /// <param name="outerNodes">List of PolygonSplittingGraphNodes in order forming an outer perimeter.</param>
    /// <param name="polyToTileMap">A Dictionary mapping PolyEdges to their respective TileEdges.</param>
    /// <returns>List of PolyEdges forming the same outer perimeter described by <param>outerNodes</param>.</returns>
    private static List<TileEdge> _ConvertPolygonNodesToPolyEdges(List<PolygonSplittingGraphNode> outerNodes, Dictionary<PolyEdge, TileEdge> polyToTileMap)
    {
        var outerEdges = new List<TileEdge>();
        for (int i = 0; i < outerNodes.Count - 1; i++)
        {
            var thisVertex = new Vector2(outerNodes[i].x, outerNodes[i].y);
            var nextVertex = new Vector2(outerNodes[i + 1].x, outerNodes[i + 1].y);
            foreach (PolyEdge polyEdge in polyToTileMap.Keys)
            {
                if (polyEdge.a == thisVertex && polyEdge.b == nextVertex ||
                    polyEdge.b == thisVertex && polyEdge.a == nextVertex)
                {
                    TileEdge tileEdge = polyToTileMap[polyEdge];
                    if (polyEdge.a == thisVertex) outerEdges.Add(tileEdge);
                    else
                    {
                        TileEdge reverseEdge = tileEdge.GetReverseEdge() as TileEdge;
                        outerEdges.Add(reverseEdge);
                    }
                }
            }
        }
        return outerEdges;
    }

    private static List<TileEdge> _ConvertChordlessPolygonToPolyEdges(Vector2[] outerPerim, Dictionary<PolyEdge, TileEdge> polyToTileMap)
    {
        var outerEdges = new List<TileEdge>();
        for (int i = 0; i < outerPerim.Length - 1; i++)
        {
            Vector2 thisVertex = outerPerim[i];
            Vector2 nextVertex = outerPerim[i + 1];
            foreach (PolyEdge polyEdge in polyToTileMap.Keys)
            {
                if (polyEdge.a == thisVertex && polyEdge.b == nextVertex ||
                    polyEdge.b == thisVertex && polyEdge.a == nextVertex)
                {
                    TileEdge tileEdge = polyToTileMap[polyEdge];
                    if (polyEdge.a == thisVertex) outerEdges.Add(tileEdge);
                    else
                    {
                        TileEdge reverseEdge = tileEdge.GetReverseEdge() as TileEdge;
                        outerEdges.Add(reverseEdge);
                    }
                }
            }
        }
        return outerEdges;
    }
}
}