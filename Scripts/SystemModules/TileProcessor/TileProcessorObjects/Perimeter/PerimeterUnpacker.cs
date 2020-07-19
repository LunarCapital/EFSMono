using Godot;
using System;
using SCol = System.Collections.Generic;
using MCN = MainControllerNamespace;

namespace TileProcessorNamespace
{
/// <summary>
/// A class that unpacks the results of PerimeterBuilder into three dictionaries (that work something
/// like a NoSQL DB).
/// </summary>
public class PerimeterUnpacker
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
    public (SCol.Dictionary<EdgeCollKey, EdgeCollection> edgeCollMap,
             SCol.Dictionary<HoleGroupKey, int> holeGroupMap,
             SCol.Dictionary<TileGroupKey, int> tileGroupMap) UnpackEdgeCols(
                                SCol.Dictionary<TileMap, SCol.List<EdgeCollection>> tileMapToAllEdgeColls,
                                MCN.TileMapList tileMaps)
    {
        var edgeCollMap = new SCol.Dictionary<EdgeCollKey, EdgeCollection>();
        var holeGroupMap = new SCol.Dictionary<HoleGroupKey, int>();
        var tileGroupMap = new SCol.Dictionary<TileGroupKey, int>();

        foreach (TileMap tileMap in tileMaps.Values)
        {
            SCol.List<EdgeCollection> allEdgeColls = tileMapToAllEdgeColls[tileMap];
            tileGroupMap.Add(new TileGroupKey(tileMap), allEdgeColls.Count);

            for (int tileGroup = 0; tileGroup < allEdgeColls.Count; tileGroup++)
            {
                EdgeCollection thisGroupsColl = allEdgeColls[tileGroup];
                SCol.List<EdgeCollection> splitEdgeColl = this._SplitEdgeColl(thisGroupsColl); //should hold perim in index 0 and holes in 1-inf
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
    /// <param name="originalEdgeCollection">Collection of all edges in a TileMap, regardless of whether they are connected or not</param>
    /// <returns>A list of Edge Collections, all ordered, and all split into connected groups</returns>
    private SCol.List<EdgeCollection> _SplitEdgeColl(EdgeCollection originalEdgeColl)
    {
        var splitEdgeColl = new SCol.List<EdgeCollection>();
        EdgeCollection cloneEdgeColl = originalEdgeColl.Clone();

        while (cloneEdgeColl.Count > 0)
        {
            EdgeCollection orderedEdgeColl = cloneEdgeColl.GetOrderedCollection();
            splitEdgeColl.Add(orderedEdgeColl);
            cloneEdgeColl = cloneEdgeColl.GetExcludedCollection(orderedEdgeColl);
        }

        return splitEdgeColl;
    }

}
}