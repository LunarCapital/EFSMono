using Godot;
using System;
using SCol = System.Collections.Generic;
using MCN = MainControllerNamespace;

namespace TileProcessorNamespace
{
/// <summary>
/// <para>
/// A class that builds Area2Ds, walls, and edges for a world based off its tilemaps.
///
/// The purpose of each:
///
/// 	Area2D: Used to help check what 'floor' the player is on.
/// 			Also lets player stand on a floor even if their centre is 'off' the floor.
/// 	Walls: 	Separators between floors of different heights.
/// 			The bottom of a tile that you cannot pass through.
/// 			EXAMPLE: Think of 3x3 ground floor tiles. In the centre is a 1x1 'first floor' tile.
/// 			If you were on the 3x3 ground floor, you would not be able to walk into the centre
/// 			because of the four walls in the way.
/// 	Ledges: Separators between tiles and the void (and the hardest to build).
///
/// Should store tilemaps in an array IN ORDER of increasing Z level.
///
/// Briefly, the stages this script goes through to build these nodes are:
/// 	Uses PerimeterBuilder to iterate through tilemaps one by one, and attempts to
/// 	mark the perimeters of the irregular polygons formed by groups of tiles being
/// 	adjacent to each other.
/// 	These perimeters are used to form Area2Ds that represent floors.
///
/// 	Uses FloorPartitioner to decompose the irregular polygons (that can have holes)
/// 	into the minimum number of rectangles for simpler geometry checks.  This one's real fun.
///
/// 	Uses LedgesArrayBuilder to decide where to place colliders separating floors
/// 	with the void, avoiding tiles that should allow you to drop off from (onto an
/// 	adjacent tile on a lower Z level).
///
/// 	Uses LedgeSuperimposer to copy and shift ledges upwards so they are valid on
/// 	higher floors too (because entities only interact with colliders on
/// 	the same tilemap.  If we did not superimpose ledges, you would be able to
/// 	drop off from a very high tile, and while falling move 'over' a ledge).
/// </para>
/// </summary>
public class TileProcessor
{
    public (MCN.TileMapList tileMaps, PerimeterData perimeterData,
            LedgeData ledgeData) BuildTileNodes(SCol.List<Node> worldChildren)
    {
        MCN.TileMapList tileMaps = this._FillTilemapsArray(worldChildren);
        PerimeterData perimData = new PerimeterData(tileMaps);
        LedgeData ledgeData = new LedgeData(perimData, tileMaps);

        // this._CreateTilemapArea2Ds(perimData, tileMaps);

        return (tileMaps, perimData, ledgeData);
    }

    /// <summary>
    /// Extracts tilemap nodes from the world node's children
    /// </summary>
    /// <param name="worldChildren">The world node's children in an array</param>
    /// <returns>An array of the world node's tilemap children</returns>
    private MCN.TileMapList _FillTilemapsArray(SCol.List<Node> worldChildren)
    {
        var tileMaps = new SCol.List<TileMap>();
        foreach (Node child in worldChildren)
        {
            if (child is TileMap tileMap)
            {
                tileMaps.Add(tileMap);
            }
        }
        return new MCN.TileMapList(tileMaps);
    }

    /// <summary>
    /// Using the perimeters stored in PerimeterData, creates Area2Ds describing the floor.
    /// </summary>
    /// <param name="perimData">Contains info on all perimeters.</param>
    /// <param name="tileMaps">List of all tilemaps.</param>
    // private void _CreateTilemapArea2Ds(PerimeterData perimData, MCN.TileMapList tileMaps)
    // {
    //     foreach (TileMap tileMap in tileMaps.Values)
    //     {
    //             var area2D = new Area2D
    //             {
    //                 Name = tileMap.Name + AREA_NAME
    //             };
    //             tileMap.AddChild(area2D);

    //         int maxTileGroups = perimData.GetMaxTileGroup(tileMap);
    //         for (int tileGroup = 0; tileGroup < maxTileGroups; tileGroup++)
    //         {
    //             int maxHoleGroups = perimData.GetMaxHoleGroup(tileMap, tileGroup);
    //             EdgeCollection[] allPerims = new EdgeCollection[maxHoleGroups];

    //             for (int holeGroup = 0; holeGroup < maxHoleGroups; holeGroup++)
    //             {
    //                 allPerims[holeGroup] = perimData.GetEdgeCollection(tileMap, tileGroup, holeGroup);
    //             }

    //             //RECTANGLE DECOMPOSITION HERE

    //         }

    //     }
    // }

}
}