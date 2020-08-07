using EFSMono.Scripts.DataStructures.Geometry;
using EFSMono.Scripts.SystemModules.GeneralUtilities;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.DataKeys;
using Godot;
using System.Collections.Generic;

namespace EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter
{
/// <summary>
/// A class that unpacks the results of PerimeterBuilder into three dictionaries (that work something
/// like a NoSQL DB).
/// </summary>
public static class PerimeterUnpacker
{
    /// <summary>
    /// Unpacks the input dictionary into the three dictionary properties in this class.
    /// To be more specific, the input dict maps a tilemap to a list of all of its edge collections
    /// for each tile group. Each edge collection contains all perimeters in a tile group including that
    /// of its holes. This func uses this class's struct keys to 'pigeonhole' each edge collection into
    /// its right place so it can be 'looked up' (like in a DB) with the keys
    /// [tilemap, tile group, hole group].
    /// </summary>
    /// <param name="tileMapToAllEdgeColls">All TileCollections from every hole group form every
    /// tile group from every tile map.</param>
    /// <param name="tileMaps">List of all TileMaps.</param>
    /// <returns>The three dictionary properties of PerimeterData.</returns>
    public static (Dictionary<EdgeCollKey, EdgeCollection<TileEdge>>, Dictionary<HoleGroupKey, int>, 
                   Dictionary<TileGroupKey, int>) UnpackEdgeCols(this TileMapList tileMaps,
        Dictionary<TileMap, List<EdgeCollection<TileEdge>>> tileMapToAllEdgeColls)
    {
        var edgeCollMap = new Dictionary<EdgeCollKey, EdgeCollection<TileEdge>>();
        var holeGroupMap = new Dictionary<HoleGroupKey, int>();
        var tileGroupMap = new Dictionary<TileGroupKey, int>();

        foreach (TileMap tileMap in tileMaps.Values)
        {
            List<EdgeCollection<TileEdge>> allEdgeColls = tileMapToAllEdgeColls[tileMap];
            tileGroupMap.Add(new TileGroupKey(tileMap), allEdgeColls.Count);

            for (int tileGroup = 0; tileGroup < allEdgeColls.Count; tileGroup++)
            {
                EdgeCollection<TileEdge> thisGroupsColl = allEdgeColls[tileGroup];
                List<EdgeCollection<TileEdge>> splitEdgeColl = _SplitEdgeColl(thisGroupsColl); //should hold perim in index 0 and holes in 1-inf
                holeGroupMap.Add(new HoleGroupKey(tileMap, tileGroup), splitEdgeColl.Count);

                for (int holeGroup = 0; holeGroup < splitEdgeColl.Count; holeGroup++)
                {
                    edgeCollMap.Add(new EdgeCollKey(tileMap, tileGroup, holeGroup), splitEdgeColl[holeGroup]);
                }
            }
        }
        return (edgeCollMap, holeGroupMap, tileGroupMap);
    }

    /// <summary>
    /// Given an EdgeCollection with a bunch of edges:
    ///     1. Grabs a group of edges that are connected to each other and orders them.
    ///        Edges not connected to this group are left behind.
    ///     2. Out of the left behind edges, grabs a group that are connected and orders them.
    ///        Again, edges not connected to this group are left behind.
    ///     3. Repeat until all edges are ordered and split into their own connected groups.
    /// Returns the split and ordered edges in a List.
    /// </summary>
    /// <param name="originalEdgeColl">Collection of all edges in a TileMap, regardless of whether they are connected or not</param>
    /// <returns>A list of Edge Collections, all ordered, and all split into connected groups</returns>
    private static List<EdgeCollection<TileEdge>> _SplitEdgeColl(EdgeCollection<TileEdge> originalEdgeColl)
    {
        var splitEdgeColl = new List<EdgeCollection<TileEdge>>();
        EdgeCollection<TileEdge> cloneEdgeColl = originalEdgeColl.Clone();
        EdgeCollection<TileEdge> outerEdgesColl = cloneEdgeColl.GetOuterClosedLoop(); //first pull the outer perim
        foreach (TileEdge edge in outerEdgesColl)
        {
            GD.PrintS("outer edge has edge: " + edge.a + " to " + edge.b);
        }
        splitEdgeColl.Add(outerEdgesColl);
        cloneEdgeColl = cloneEdgeColl.GetExcludedCollection(outerEdgesColl);
        while (cloneEdgeColl.Count > 0)
        {
            List<EdgeCollection<TileEdge>> smallClosedLoops = cloneEdgeColl.GetSmallClosedLoops();
            foreach (EdgeCollection<TileEdge> smallClosedLoop in smallClosedLoops)
            {
                foreach (TileEdge edge in smallClosedLoop)
                {
                    GD.PrintS("small loop has edge: " + edge.a + " to " + edge.b);
                }
                splitEdgeColl.Add(smallClosedLoop);
                cloneEdgeColl = cloneEdgeColl.GetExcludedCollection(smallClosedLoop);
            }
        }
        return splitEdgeColl;
    }

}
}