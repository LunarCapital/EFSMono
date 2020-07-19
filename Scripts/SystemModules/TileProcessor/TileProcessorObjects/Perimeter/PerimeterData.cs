using Godot;
using System;
using SCol = System.Collections.Generic;
using MCN = MainControllerNamespace;

namespace TileProcessorNamespace
{
/// <summary>
/// <para>
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
/// </para>
/// </summary>
public class PerimeterData
{
    private readonly SCol.Dictionary<EdgeCollKey, EdgeCollection> _edgeCollMap;
    private readonly SCol.Dictionary<HoleGroupKey, int> _holeGroupMap;
    private readonly SCol.Dictionary<TileGroupKey, int> _tileGroupMap;

    public PerimeterData(MCN.TileMapList tileMaps)
    {
        PerimeterBuilder perimBuilder = new PerimeterBuilder();
        PerimeterUnpacker perimUnpacker = new PerimeterUnpacker();
        SCol.Dictionary<TileMap, SCol.List<EdgeCollection>> tileMapToAllEdgeCols = perimBuilder.BuildPerimeter(tileMaps);
        (_edgeCollMap, _holeGroupMap, _tileGroupMap) = perimUnpacker.UnpackEdgeCols(tileMapToAllEdgeCols, tileMaps);
    }

    public EdgeCollection GetEdgeCollection(TileMap tileMap, int tileGroup, int holeGroup)
    {
        return this._edgeCollMap[new EdgeCollKey(tileMap, tileGroup, holeGroup)];
    }

    public int GetMaxHoleGroup(TileMap tileMap, int tileGroup)
    {
        return this._holeGroupMap[new HoleGroupKey(tileMap, tileGroup)];
    }

    public int GetMaxTileGroup(TileMap tileMap)
    {
        return this._tileGroupMap[new TileGroupKey(tileMap)];
    }

}
}