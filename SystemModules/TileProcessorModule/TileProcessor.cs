using System;
using System.Collections.Generic;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Ledge;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter;
using Godot;

namespace EFSMono.SystemModules.TileProcessorModule
{
    /// <summary>
    /// A class that builds Area2Ds, walls, and edges for a world based off its TileMaps.
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
    /// Should store TileMaps in an array IN ORDER of increasing Z level.
    ///
    /// Briefly, the stages this script goes through to build these nodes are:
    /// 	Uses PerimeterBuilder to iterate through TileMaps one by one, and attempts to
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
    /// 	the same TileMap.  If we did not superimpose ledges, you would be able to
    /// 	drop off from a very high tile, and while falling move 'over' a ledge).
    /// </summary>
    public static class TileProcessor
    {
        public static (TileMapList tileMaps, PerimeterData perimeterData,
                LedgeData ledgeData) BuildTileNodes(IEnumerable<Node> worldChildren)
        {
            if (worldChildren is null) throw new ArgumentNullException(nameof(worldChildren));

            TileMapList tileMaps = _FillTilemapsArray(worldChildren);
            var perimData = new PerimeterData(tileMaps);
            var ledgeData = new LedgeData(perimData, tileMaps);
            return (tileMaps, perimData, ledgeData);
        }

        /// <summary>
        /// Extracts TileMap nodes from the world node's children
        /// </summary>
        /// <param name="worldChildren">The world node's children in an array</param>
        /// <returns>An array of the world node's TileMap children</returns>
        private static TileMapList _FillTilemapsArray(IEnumerable<Node> worldChildren)
        {
            var tileMaps = new List<TileMap>();
            foreach (Node child in worldChildren)
            {
                if (child is TileMap tileMap)
                {
                    tileMaps.Add(tileMap);
                }
            }
            return new TileMapList(tileMaps);
        }
    }
}