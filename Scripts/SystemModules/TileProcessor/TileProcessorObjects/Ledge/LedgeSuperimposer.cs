using Godot;
using System;
using SCol = System.Collections.Generic;
using MCN = MainControllerNamespace;
using AutoloadNamespace;

namespace TileProcessorNamespace
{
/// <summary>
/// Like the name?  I don't.
///
/// <para>
/// This class takes ledges and 'superimposes' them onto tilemaps of higher heights.
/// Consider a 3x3 island on 0 height level.  It has 12 ledges all around its perimeter.
/// Now consider that the island has a 1-wide tile pillar up to N height in its centre.  You could,
/// theoretically, as a player, be on top of the pillar and jump off into the void because the
/// 3x3's ledges only apply to an entity on the SAME HEIGHT LEVEL.
///</para>
///
/// Thus we need to take those ledges of the 3x3 island and
/// apply them to EVERY SINGLE TILEMAP OF HIGHER HEIGHT.
///
/// However, we do not superimpose a ledge to a higher tilemap IF, on the higher tilemap, there is
/// a tile in one of TWO places:
/// 	1. Directly above the tile that originally spawned the edge
/// 	2. Or adjacent to the above tile in the DIRECTION THAT THE EDGE IS IN.
///
/// In this case, the ledge is not applied to the higher tilemap.  Additionally, that same ledge
/// is also not applied to any tilemaps higher than that one either.
///
/// A consequence of a ledge not being applied to a higher tilemap is that ledge groups can change.
/// Consider the same 3x3 island in the first example with the infinite pillar in the middle, where the
/// 3x3's 12 ledges are superimposed ALL the way upwards. Now we change the example so somewhere in the
/// middle of the pillar there are two tiles on opposite sides of the pillar.
///
/// These two tiles being present means that from that point onwards, only 10 ledges need to be superimposed.
/// HOWEVER, these ledges have been separated by the two tiles, and now instead of one group of 12 ledges,
/// we have two groups of 5 ledges each.
///
/// This potential change in # of ledge groups is addressed in this class as well.
/// </summary>
public class LedgeSuperimposer
{

    private const int SHIFT_DIST = 1;

    /// <summary>
    /// <para>
    /// Given the LedgeData dicts that have had their initial layers built, superimposes all tilemap
    /// ledges to every single existing tilemap on a higher level, one by one.
    ///
    /// For example, consider a map with three tilemaps of different heights, 0, 1, and 2.
    /// First we superimpose TM0 to TM1 (which we can call TM1_from_0), then superimpose TM1_from_0 to TM2.
    /// Next we superimpose TM1 to TM2.
    /// Note that we superimposed from TM1 to TM2 twice, because both TM0 and TM1 could have
    /// ledges that need to be superimposed separately to TM2.
    /// </para>
    /// </summary>
    /// <param name="ledgeCollMap">Dict of ledge group to ledge EdgeCollection.</param>
    /// <param name="ledgeGroupMap"></param>
    /// <param name="holeGroupMap"></param>
    /// <param name="tileGroupMap"></param>
    /// <param name="tileMaps"></param>
    /// <returns></returns>
    public (SCol.Dictionary<LedgeCollKey, EdgeCollection> ledgeCollMap,
            SCol.Dictionary<LedgeGroupKey, int> ledgeGroupMap) SuperimposeLedges(
                                                SCol.Dictionary<LedgeCollKey, EdgeCollection> ledgeCollMap,
                                                SCol.Dictionary<LedgeGroupKey, int> ledgeGroupMap,
                                                SCol.Dictionary<HoleGroupKey, int> holeGroupMap,
                                                SCol.Dictionary<TileGroupKey, int> tileGroupMap,
                                                MCN.TileMapList tileMaps)
    {
        var ledgeCollMapClone = new SCol.Dictionary<LedgeCollKey, EdgeCollection>(ledgeCollMap);
        var ledgeGroupMapClone = new SCol.Dictionary<LedgeGroupKey, int>(ledgeGroupMap);

        foreach (TileMap baseTileMap in tileMaps.Values)
        {
            for (int t = baseTileMap.ZIndex + 1; t < tileMaps.Count; t++)
            {
                TileMap superTileMap = tileMaps[t];
                TileMap preTileMap = tileMaps[t - 1]; //needed because superimposition is done in sequence, from T to T+1 then T+2, etc. We don't jump from T to T+10

                int maxTileGroups = tileGroupMap[new TileGroupKey(baseTileMap)];
                for (int tileGroup = 0; tileGroup < maxTileGroups; tileGroup++)
                {
                    int maxHoleGroups = holeGroupMap[new HoleGroupKey(baseTileMap, tileGroup)];
                    for (int holeGroup = 0; holeGroup < maxHoleGroups; holeGroup++)
                    {
                        (ledgeCollMapClone, ledgeGroupMapClone) = this._SuperimposeHoleGroup(ledgeCollMapClone, ledgeGroupMapClone,
                                                                                             baseTileMap, superTileMap, preTileMap,
                                                                                             tileGroup, holeGroup);
                    }
                }
            }
        }

        return (ledgeCollMapClone, ledgeGroupMapClone);
    }

    /// <summary>
    /// Superimposes ONE hole group's ledge groups.
    /// There isn't much else to say, so check out that disgusting number of input parameters.
    /// Hopefully nobody on earth ever looks at this except me.
    /// </summary>
    /// <param name="ledgeCollMap"></param>
    /// <param name="ledgeGroupMap"></param>
    /// <param name="baseTileMap"></param>
    /// <param name="superTileMap"></param>
    /// <param name="preTileMap"></param>
    /// <param name="tileGroup"></param>
    /// <param name="holeGroup"></param>
    /// <returns></returns>
    private (SCol.Dictionary<LedgeCollKey, EdgeCollection> ledgeCollMap,
            SCol.Dictionary<LedgeGroupKey, int> ledgeGroupMap) _SuperimposeHoleGroup(
                                                SCol.Dictionary<LedgeCollKey, EdgeCollection> ledgeCollMap,
                                                SCol.Dictionary<LedgeGroupKey, int> ledgeGroupMap,
                                                TileMap baseTileMap,
                                                TileMap superTileMap,
                                                TileMap preTileMap,
                                                int tileGroup,
                                                int holeGroup)
    {
        var ledgeCollMapClone = new SCol.Dictionary<LedgeCollKey, EdgeCollection>(ledgeCollMap);
        var ledgeGroupMapClone = new SCol.Dictionary<LedgeGroupKey, int>(ledgeGroupMap);

        int superLedgeGroup = 0; //Once superimposed, # of ledge groups CAN change
        int maxLedgeGroups = ledgeGroupMap[new LedgeGroupKey(baseTileMap, tileGroup, holeGroup, preTileMap)];
        for (int ledgeGroup = 0; ledgeGroup < maxLedgeGroups; ledgeGroup++)
        {
            EdgeCollection ledgeColl = ledgeCollMap[new LedgeCollKey(baseTileMap, tileGroup, holeGroup, preTileMap, ledgeGroup)];
            EdgeCollection validLedges = this._BuildHoleGroupValidLedges(ledgeColl, preTileMap, superTileMap);

            while (validLedges.Count > 0)
            { //repeatedly GetOrdereGroup() on valid_ledges until no ledges are left behind
               EdgeCollection orderedLedges = validLedges.GetOrderedCollection();
               var validSet = new SCol.HashSet<Edge>(validLedges);
               validSet.ExceptWith(orderedLedges);
               validLedges = new EdgeCollection(validSet);
               ledgeCollMapClone.Add(new LedgeCollKey(baseTileMap, tileGroup, holeGroup, superTileMap, superLedgeGroup), orderedLedges);
               superLedgeGroup++;
            }
        }
        ledgeGroupMapClone.Add(new LedgeGroupKey(baseTileMap, tileGroup, holeGroup, superTileMap), superLedgeGroup);
        return (ledgeCollMapClone, ledgeGroupMapClone);
    }

    /// <summary>
    /// Given some input EdgeCollection, iterates through its edges and adds 'valid' ones to a new
    /// collection, which is then returned.
    /// Validity is determined through the _IsLedgeValid function.
    /// </summary>
    /// <param name="ledgeColl"></param>
    /// <param name="preTileMap"></param>
    /// <param name="superTileMap"></param>
    /// <returns></returns>
    private EdgeCollection _BuildHoleGroupValidLedges(EdgeCollection ledgeColl, TileMap preTileMap, TileMap superTileMap)
    {
        EdgeCollection validLedges = new EdgeCollection();
        foreach (Edge ledge in ledgeColl)
        {
            if (this._IsLedgeValid(ledge, preTileMap, superTileMap))
            {
                Edge superimposedLedge = ledge.GetShiftedByN(SHIFT_DIST);
                if (!validLedges.HasEdgePoints(superimposedLedge))
                {
                    validLedges.Add(superimposedLedge);
                }
            }
        }
        return validLedges;
    }

    /// <summary>
    /// Checks if a ledge would be valid if superimposed to a higher tilemap by checking if said higher
    /// tilemap contains a tile either:
    ///     1. Above (on the superimposed tile map) the tile that the ledge originated from (on base tilemap)
    ///     2. Above and adjacent (on superimposed tilemap) the tile that the ledge originated from IN THE
    ///        DIRECTION that the ledge is in.
    /// </summary>
    /// <param name="ledge">Ledge being checked for validity.</param>
    /// <param name="baseTileMap">TileMap that the ledge is on.</param>
    /// <param name="superTileMap">TileMap that ledge would be superimposed to if valid.</param>
    /// <returns>True is valid, false otherwise.</returns>
    private bool _IsLedgeValid(Edge ledge, TileMap baseTileMap, TileMap superTileMap)
    {
        Vector2 aboveTile = TileFuncs.GetTileAboveOrBelow(ledge.tileCoords, baseTileMap.ZIndex, superTileMap.ZIndex);
        Vector2 adjAboveTile = TileFuncs.GetTileAdjacent(aboveTile, ledge.tileSide);

        return superTileMap.GetCellv(aboveTile) == TileMap.InvalidCell &&
                superTileMap.GetCellv(adjAboveTile) == TileMap.InvalidCell;
    }

}
}