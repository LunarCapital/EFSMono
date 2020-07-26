using EFSMono.Scripts.SystemModules.GeneralUtilities;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.DataKeys;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter;
using Godot;
using SCol = System.Collections.Generic;

namespace EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Ledge
{
/// <summary>
/// A class created by the LedgeBuilder class and 'completed' by the LedgeSuperimposer class.
/// Contains info about edge perimeters (including hole edge perimeters on the inside) of tile groups,
/// which we dub 'Ledges'. Ledges are either:
///     1. The separator between a tile and the void (AKA no tile adjacent in that direction), in
///        which case the ledge acts as a wall.
///     2. The separator between a tile on height N and a tile on height M &lt; N, in which case the
///        ledge should allow an entity to pass through the edge and fall to the adjacent tile on
///         height M.
/// In other words, ledges can be walls or passable space depending on the tile configuration. Additionally,
/// tile perimeters may NOT be comprised of the same type of ledge. Consider a single tile alone in space. It
/// has four edges in its perimeter, and thus has four ledges. Currently, all four ledges are walls. If a tile
/// on a LOWER HEIGHT is placed adjacent to the tile, now three of its ledges are walls and one ledge can be 
/// passed through.
/// 
/// AS SUCH, we now need an additional field: Ledge Groups. Ledges are grouped together by type. In the above
/// example, there is one ledge group, comprised of the three wall ledges. We do not bother grouping the
/// passable edge.
/// 
/// IF we changed the example by adding another tile of LOWER HEIGHT adjacent to the tile on the OPPOSITE SIDE
/// of the existing adjacent tile, now it has TWO ledge groups: 
///     1. one wall ledge
///     2. one wall ledge on the opposite side
/// 
/// Finally, ledges need to be superimposed. A proper explanation will be on the LedgeSuperimposer class, but a 
/// brief description is that a ledge WALL on height N at some coordinate (x, y) needs to be 'copied' to all heights
/// above N unless some height M > N has a tile on coordinate (x, y) OR adjacent to that coordinate in the direction
/// of the ledge. If a height does not copy a lower height's ledge wall is NOT copied to a higher height M, then all
/// heights ABOVE M do not need to have that ledge wall copied to them either.
///
/// Quick description of each key:
///     1. tileMap: As with PerimeterData, a TileMap represents a height.
///     2. tileGroup: Tiles within a TileMap may be disconnected from each other. As such, these tiles are in different groups.
///     3. holeGroup: A tile group may have holes.
///     4. superMap: Ledge walls need to be superimposed from the tileMap to EVERY HIGHER tileMap, AKA superMap.
///     5. ledgeGroup: Ledge walls around a perimeter may be 'separated' by gaps of passable ledges and thus are placed in diff groups.
/// </summary>
public class LedgeData
{
    private readonly SCol.Dictionary<LedgeCollKey, EdgeCollection> _ledgeCollMap;
    private readonly SCol.Dictionary<LedgeGroupKey, int> _ledgeGroupMap;
    private readonly SCol.Dictionary<HoleGroupKey, int> _holeGroupMap;
    private readonly SCol.Dictionary<TileGroupKey, int> _tileGroupMap;

    public LedgeData(PerimeterData perimData, TileMapList tileMaps)
    {
        (this._ledgeCollMap, this._ledgeGroupMap, this._holeGroupMap, this._tileGroupMap) = tileMaps.BuildLedges(perimData);
        (this._ledgeCollMap, this._ledgeGroupMap) = tileMaps.SuperimposeLedges(this._ledgeCollMap, this._ledgeGroupMap,
                                                                               this._holeGroupMap, this._tileGroupMap);
    }

    public EdgeCollection GetLedgeCollection(TileMap tileMap, int tileGroup, int holeGroup,
                                             TileMap superTileMap, int ledgeGroup)
    {
        return this._ledgeCollMap[new LedgeCollKey(tileMap, tileGroup, holeGroup, superTileMap, ledgeGroup)];
    }

    public int GetMaxLedgeGroup(TileMap tileMap, int tileGroup, int holeGroup, TileMap superTileMap)
    {
        return this._ledgeGroupMap[new LedgeGroupKey(tileMap, tileGroup, holeGroup, superTileMap)];
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