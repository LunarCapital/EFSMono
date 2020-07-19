using Godot;
using SCol = System.Collections.Generic;
using MCN = MainControllerNamespace;
using TPN = TileProcessorNamespace;
using AutoloadNamespace;

namespace PhysicsControllerNamespace {
/// <summary>
/// Makes Physics RIDs for static bodies for walls and ledges, and also makes the
/// collision shapes for those bodies.
/// </summary>
public class CollisionConstructor
{
    public enum STATIC_BODY_ORIGIN {WALL = 0, LEDGE = 1};

    public SCol.Dictionary<TileMap, RID> ConstructTileMapFloorMap(MCN.TileMapList tileMaps, RID worldSpace)
    {
        var tileMapToFloorArea2D = new SCol.Dictionary<TileMap, RID>();
        foreach (TileMap tileMap in tileMaps.Values)
        {
            RID area2d = Physics2DServer.AreaCreate();
            Physics2DServer.AreaSetSpace(area2d, worldSpace);

        }
        return tileMapToFloorArea2D;
    }

    /// <summary>
    /// Constructs a dictionary that maps a TileMap to a Physics2DServer-created StaticBody2D (RID).
    /// Is used for both the walls and ledges dictionaries.
    /// </summary>
    /// <param name="tileMaps">List of TileMaps.</param>
    /// <param name="worldSpace">RID of the World Space.</param>
    /// <returns>A Dictionary that maps TileMaps to an RID of a StaticBody2D.</returns>
    public SCol.Dictionary<TileMap, RID> ConstructTileMapCollisionMap(MCN.TileMapList tileMaps, RID worldSpace)
    {
        var tileMapToSB2Ds = new SCol.Dictionary<TileMap, RID>();
        foreach (TileMap tileMap in tileMaps.Values)
        {
            RID edgeSB2D = Physics2DServer.BodyCreate();
            Physics2DServer.BodySetMode(edgeSB2D, Physics2DServer.BodyMode.Static);
            Physics2DServer.BodySetSpace(edgeSB2D, worldSpace);
            Physics2DServer.BodySetState(edgeSB2D, Physics2DServer.BodyState.Transform, Transform2D.Identity);
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
    public SCol.Dictionary<RID, SCol.List<SegmentShape2D>> ConstructSB2DSegments(MCN.TileMapList tileMaps,
                                                                       SCol.Dictionary<TileMap, RID> tileMapToSB2Ds,
                                                                       TPN.PerimeterData perimData,
                                                                       TPN.LedgeData ledgeData,
                                                                       STATIC_BODY_ORIGIN option)
    {
        var sb2dToSegments = new SCol.Dictionary<RID, SCol.List<SegmentShape2D>>();
        foreach (TileMap tileMap in tileMaps.Values)
        {
            RID wall = tileMapToSB2Ds[tileMap];
            SCol.List<SegmentShape2D> segments = (option == STATIC_BODY_ORIGIN.WALL) ?
                                                  this._GetWallSegments(tileMaps, tileMap, perimData) :
                                                  this._GetLedgeSegments(tileMaps, tileMap, ledgeData);
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
    private SCol.List<SegmentShape2D> _GetWallSegments(MCN.TileMapList tileMaps, TileMap thisTileMap, TPN.PerimeterData perimData)
    {
        SCol.List<SegmentShape2D> allSegments = new SCol.List<SegmentShape2D>();
        if (thisTileMap != tileMaps.Last())
        { //there are never walls on the highest tilemap
            int nextIndex = thisTileMap.ZIndex + 1;
            TileMap nextTileMap = tileMaps[nextIndex];

            int maxTileGroups = perimData.GetMaxTileGroup(nextTileMap);
            for (int tileGroup = 0; tileGroup < maxTileGroups; tileGroup++)
            {
                int maxHoleGroups = perimData.GetMaxHoleGroup(nextTileMap, tileGroup);
                for (int holeGroup = 0; holeGroup < maxHoleGroups; holeGroup++)
                {
                    TPN.EdgeCollection wallColl = perimData.GetEdgeCollection(nextTileMap, tileGroup, holeGroup);
                    SCol.List<SegmentShape2D> thisWallSegments = this._ShiftSegmentsDown(this._EdgeCollToSegments(wallColl));
                    allSegments.AddRange(thisWallSegments);
                }
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
    private SCol.List<SegmentShape2D> _GetLedgeSegments(MCN.TileMapList tileMaps, TileMap thisTileMap, TPN.LedgeData ledgeData)
    {
        SCol.List<SegmentShape2D> allSegments = new SCol.List<SegmentShape2D>();
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
                        TPN.EdgeCollection ledgeColl = ledgeData.GetLedgeCollection(baseTileMap, tileGroup, holeGroup, thisTileMap, ledgeGroup);
                        SCol.List<SegmentShape2D> thisLedgeSegments = this._EdgeCollToSegments(ledgeColl);
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
    private SCol.List<SegmentShape2D> _EdgeCollToSegments(TPN.EdgeCollection edgeColl)
    {
        SCol.List<SegmentShape2D> segments = new SCol.List<SegmentShape2D>();
        SCol.List<Vector2> simplifiedPolygon = edgeColl.GetSimplifiedPerim();
        for (int thisIndex = 1; thisIndex < simplifiedPolygon.Count; thisIndex++)
        {
            int prevIndex = thisIndex - 1;
            SegmentShape2D segment = new SegmentShape2D
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
    /// tilemap to a lower one so that they are in the right place.
    /// </summary>
    /// <param name="segments">Segments that are being shifted down.</param>
    /// <returns>A clone of the segments list input, but shifted down.</returns>
    private SCol.List<SegmentShape2D> _ShiftSegmentsDown(SCol.List<SegmentShape2D> segments)
    {
        SCol.List<SegmentShape2D> segmentsClone = new SCol.List<SegmentShape2D>(segments);
        for (int i = 0; i < segmentsClone.Count; i++)
        {
                segmentsClone[i].A += new Vector2(0, Globals.TILE_HEIGHT);
                segmentsClone[i].B += new Vector2(0, Globals.TILE_HEIGHT);
        }
        return segmentsClone;
    }
}
}