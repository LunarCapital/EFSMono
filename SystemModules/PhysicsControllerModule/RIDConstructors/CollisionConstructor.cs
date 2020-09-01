using System;
using System.Collections.Generic;
using EFSMono.Common.Autoload;
using EFSMono.Common.DataStructures.Geometry;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Ledge;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter;
using Godot;
using static EFSMono.Common.Autoload.LayersFuncs.CollisionLayers;

namespace EFSMono.SystemModules.PhysicsControllerModule.RIDConstructors
{
    /// <summary>
    /// Makes Physics RIDs for static bodies for walls and ledges, and also makes the
    /// collision shapes for those bodies.
    /// </summary>
    public static class CollisionConstructor
    {
        public enum StaticBodyOrigin { Wall = 0, Ledge = 1 };

        /// <summary>
        /// Constructs a dictionary that maps a TileMap to a Physics2DServer-created StaticBody2D (RID).
        /// Is used for both the walls and ledges dictionaries.
        /// </summary>
        /// <param name="tileMaps">List of TileMaps.</param>
        /// <param name="worldSpace">RID of the World Space.</param>
        /// <returns>A Dictionary that maps TileMaps to an RID of a StaticBody2D.</returns>
        public static Dictionary<TileMap, RID> ConstructTileMapCollisionMap(this TileMapList tileMaps, RID worldSpace)
        {
            if (tileMaps is null) throw new ArgumentNullException(nameof(tileMaps));

            var tileMapToSB2Ds = new Dictionary<TileMap, RID>();
            foreach (TileMap tileMap in tileMaps.Values)
            {
                RID edgeSB2D = Physics2DServer.BodyCreate();
                Physics2DServer.BodySetMode(edgeSB2D, Physics2DServer.BodyMode.Static);
                Physics2DServer.BodySetSpace(edgeSB2D, worldSpace);
                Physics2DServer.BodySetState(edgeSB2D, Physics2DServer.BodyState.Transform, Transform2D.Identity);
                Physics2DServer.BodySetCollisionLayer(edgeSB2D, LayersFuncs.GetLayersValue(Terrain));
                Physics2DServer.BodySetCollisionMask(edgeSB2D, LayersFuncs.GetLayersValue(PlayerEntity, NpcEntity));
                tileMapToSB2Ds.Add(tileMap, edgeSB2D);
            }
            return tileMapToSB2Ds;
        }

        /// <summary>
        /// Creates and returns a dictionary that maps a TileMap's StaticBody2D's RID to said TileMap's walls or ledges.
        /// </summary>
        /// <param name="tileMaps">List of TileMaps.</param>
        /// <param name="tileMapToSB2Ds">Map of TileMap to its SB2D, whether its origin was a wall or ledge.</param>
        /// <param name="perimData">Data of all perimeters of all TileMaps.</param>
        /// <param name="ledgeData">Data of all ledges of all TileMaps.</param>
        /// <param name="option">Whether this function is being used to construct segments for walls or ledges.</param>
        /// <returns>A dictionary that maps a TileMap's SB2D RID to said TileMap's walls.</returns>
        public static Dictionary<RID, List<SegmentShape2D>> ConstructSB2DSegments(this TileMapList tileMaps,
                                                                                  Dictionary<TileMap, RID> tileMapToSB2Ds,
                                                                                  PerimeterData perimData,
                                                                                  LedgeData ledgeData,
                                                                                  StaticBodyOrigin option)
        {
            if (tileMaps is null) throw new ArgumentNullException(nameof(tileMaps));
            if (tileMapToSB2Ds is null) throw new ArgumentNullException(nameof(tileMapToSB2Ds));
            if (perimData is null) throw new ArgumentNullException(nameof(perimData));
            if (ledgeData is null) throw new ArgumentNullException(nameof(ledgeData));
            
            var sb2dToSegments = new Dictionary<RID, List<SegmentShape2D>>();
            foreach (TileMap tileMap in tileMaps.Values)
            {
                RID wall = tileMapToSB2Ds[tileMap];
                List<SegmentShape2D> segments = (option == StaticBodyOrigin.Wall) ?
                                                      _GetWallSegments(tileMaps, tileMap, perimData) :
                                                      _GetLedgeSegments(tileMaps, tileMap, ledgeData);
                sb2dToSegments.Add(wall, segments);
                foreach (SegmentShape2D segment in segments)
                {
                    Physics2DServer.BodyAddShape(wall, segment.GetRid());
                }
            }
            return sb2dToSegments;
        }

        ///////////////////////////
        ///////////////////////////
        ////PRIVATE FUNCS BELOW////
        ///////////////////////////
        ///////////////////////////

        /// <summary>
        /// For a given TileMap, gets its walls as a list of SegmentShape2Ds by getting perimData's EdgeCollections for the
        /// TileMap directly above the given TileMap.
        /// </summary>
        /// <param name="tileMaps">TileMapList of TileMaps.</param>
        /// <param name="thisTileMap">The TileMap that this function is getting walls for.</param>
        /// <param name="perimData">Data of all perimeters of all TileMaps in the currently loaded world.</param>
        /// <returns>A List of SegmentShape2Ds that contain every segment that make up every wall in thisTileMap.</returns>
        private static List<SegmentShape2D> _GetWallSegments(TileMapList tileMaps, TileMap thisTileMap, PerimeterData perimData)
        {
            var allSegments = new List<SegmentShape2D>();
            if (thisTileMap == tileMaps.Last()) return allSegments; //there are never walls on the highest TileMap

            int nextIndex = thisTileMap.ZIndex + 1;
            TileMap nextTileMap = tileMaps[nextIndex];
            int maxTileGroups = perimData.GetMaxTileGroup(nextTileMap);
            for (int tileGroup = 0; tileGroup < maxTileGroups; tileGroup++)
            {
                int maxHoleGroups = perimData.GetMaxHoleGroup(nextTileMap, tileGroup);
                for (int holeGroup = 0; holeGroup < maxHoleGroups; holeGroup++)
                {
                    EdgeCollection<TileEdge> wallColl = perimData.GetEdgeCollection(nextTileMap, tileGroup, holeGroup);
                    IEnumerable<SegmentShape2D> thisWallSegments = _ShiftSegmentsDown(_EdgeCollToSegments(wallColl));
                    allSegments.AddRange(thisWallSegments);
                }
            }
            return allSegments;
        }

        /// <summary>
        /// For a given TileMap, gets its ledges as a list of SegmentShape2Ds by iterating through LedgeData for:
        ///     1. The given TileMap's own ledges
        ///     2. Ledges that have been superimposed from every lower TileMap to the given TileMap
        /// </summary>
        /// <param name="tileMaps">List of TileMaps.</param>
        /// <param name="thisTileMap">The TileMap this function is getting ledges for.</param>
        /// <param name="ledgeData">Data of all ledges of all TileMaps.</param>
        /// <returns>A list of SegmentShape2Ds that contain every segment of every ledge that is on thisTileMap.</returns>
        private static List<SegmentShape2D> _GetLedgeSegments(TileMapList tileMaps, TileMap thisTileMap, LedgeData ledgeData)
        {
            var allSegments = new List<SegmentShape2D>();
            for (int zIndex = 0; zIndex <= thisTileMap.ZIndex; zIndex++)
            {
                TileMap baseTileMap = tileMaps[zIndex];
                int maxTileGroups = ledgeData.GetMaxTileGroup(baseTileMap);
                for (int tileGroup = 0; tileGroup < maxTileGroups; tileGroup++)
                {
                    int maxHoleGroups = ledgeData.GetMaxHoleGroup(baseTileMap, tileGroup);
                    for (int holeGroup = 0; holeGroup < maxHoleGroups; holeGroup++)
                    {
                        int maxLedgeGroups = ledgeData.GetMaxLedgeGroup(baseTileMap, tileGroup, holeGroup, thisTileMap);
                        for (int ledgeGroup = 0; ledgeGroup < maxLedgeGroups; ledgeGroup++)
                        {
                            EdgeCollection<TileEdge> ledgeColl = ledgeData.GetLedgeCollection(baseTileMap, tileGroup, holeGroup, thisTileMap, ledgeGroup);
                            IEnumerable<SegmentShape2D> thisLedgeSegments = _EdgeCollToSegments(ledgeColl);
                            allSegments.AddRange(thisLedgeSegments);
                        }
                    }
                }
            }
            return allSegments;
        }

        /// <summary>
        /// Converts an EdgeCollection to a List of SegmentShape2Ds that make up that EdgeCollection,
        /// whether it is an open or closed loop.
        /// </summary>
        /// <param name="edgeColl">EdgeCollection to be converted to a list of segments</param>
        /// <returns>A list of segments that make up the edges in the input EdgeCollection</returns>
        private static IEnumerable<SegmentShape2D> _EdgeCollToSegments(EdgeCollection<TileEdge> edgeColl)
        {
            var segments = new List<SegmentShape2D>();
            List<Vector2> simplifiedPolygon = edgeColl.GetSimplifiedPerim();
            for (int thisIndex = 1; thisIndex < simplifiedPolygon.Count; thisIndex++)
            {
                int prevIndex = thisIndex - 1;
                var segment = new SegmentShape2D
                {
                    A = simplifiedPolygon[prevIndex],
                    B = simplifiedPolygon[thisIndex],
                };
                segments.Add(segment);
            }
            return segments;
        }

        /// <summary>
        /// Shifts segments down by Globals.TILE_HEIGHT. Used for moving colliders for WALLS on a higher
        /// TileMap to a lower one so that they are in the right place.
        /// </summary>
        /// <param name="segments">Segments that are being shifted down.</param>
        /// <returns>A clone of the segments list input, but shifted down.</returns>
        private static IEnumerable<SegmentShape2D> _ShiftSegmentsDown(IEnumerable<SegmentShape2D> segments)
        {
            var segmentsClone = new List<SegmentShape2D>(segments);
            foreach (SegmentShape2D segment in segmentsClone)
            {
                segment.A += new Vector2(0, Globals.TileHeight);
                segment.B += new Vector2(0, Globals.TileHeight);
            }
            return segmentsClone;
        }
    }
}