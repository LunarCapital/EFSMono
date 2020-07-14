using Godot;
using System;
using SCol = System.Collections.Generic;

namespace TileControllerNamespace
{
/// <summary>
/// A class containing the data about the perimeters built from edges drawn in Godot's tilemaps.
/// A quick explanation of each key:
///     1. tileMap: EFS has multiple tilemaps (one for each height), and we thus need to store info about each one.
///     2. tileGroup: Tiles within a tilemap may not necessarily be touching each other. The ones that do are grouped
///                   together and we need to store perimeters for each one. 
///     3. holeGroup: A group of tiles can have any number of holes, and we need to store perimeters for EACH hole.
/// 
/// We have three dictionaries.
/// One stores the # of tile groups within a tilemap.
/// One stores the # of hole groups within a tile group.
/// And the third stores the perimeter of:
///     The outside edges of a tile group IFF hole group == 0
///     The perimeter of a hole if hole group == 1 or above
/// </summary>
public class PerimeterData
{
    private SCol.Dictionary<EdgeCollKey, EdgeCollection> edgeCollMap;
    private SCol.Dictionary<HoleGroupKey, int> holeGroupMap;
    private SCol.Dictionary<TileGroupKey, int> tileGroupMap;

    public PerimeterData(TileMapList tileMaps)
    {
        PerimeterBuilder perimBuilder = new PerimeterBuilder();
        PerimeterUnpacker perimUnpacker = new PerimeterUnpacker();
        SCol.Dictionary<TileMap, SCol.List<EdgeCollection>> tileMapToAllEdgeCols = perimBuilder.BuildPerims(tileMaps);
        (edgeCollMap, holeGroupMap, tileGroupMap) = perimUnpacker.UnpackEdgeCols(tileMapToAllEdgeCols, tileMaps);
    }

    public EdgeCollection GetEdgeCollection(TileMap tileMap, int tileGroup, int holeGroup)
    {
        return this.edgeCollMap[new EdgeCollKey(tileMap, tileGroup, holeGroup)];
    }

    public int GetMaxHoleGroup(TileMap tileMap, int tileGroup)
    {
        return this.holeGroupMap[new HoleGroupKey(tileMap, tileGroup)];
    }

    public int GetMaxTileGroup(TileMap tileMap)
    {
        return this.tileGroupMap[new TileGroupKey(tileMap)];
    }

}
}
