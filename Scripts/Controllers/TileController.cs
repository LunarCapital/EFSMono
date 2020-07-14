using Godot;
using System;
using GCol = Godot.Collections;
using SCol = System.Collections.Generic;

namespace TileControllerNamespace
{
/// <summary>
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
/// </summary>
public class TileController : Node2D
{

    //CONSTANTS
    private const string AREA_NAME = "Area";

    //MODULES

    public override void _Ready()
    {

    }

    public void BuildTileNodes(SCol.List<Node> worldChildren)
    {
        TileMapList tileMaps = this._FillTilemapsArray(worldChildren);
        //PerimeterBuilderResult perimBuildResult = _perimeterBuilder.BuildPerims(tileMaps);
        PerimeterData perimData = new PerimeterData(tileMaps);
        LedgeData ledgeData = new LedgeData(perimData, tileMaps);

        this._CreateTilemapArea2Ds(perimData, tileMaps);
        this._CreateTilemapWalls(perimData, tileMaps);
    }

    /// <summary>
    /// Extracts tilemap nodes from the world node's children
    /// </summary>
    /// <param name="worldChildren">The world node's children in an array</param>
    /// <returns>An array of the world node's tilemap children</returns>
    private TileMapList _FillTilemapsArray(SCol.List<Node> worldChildren)
    {
        var tileMaps = new SCol.List<TileMap>();
        foreach (Node child in worldChildren)
        {
            if (child is TileMap)
            {
                tileMaps.Add((TileMap)child);
            }
        }
        return (new TileMapList(tileMaps));
    }

    /// <summary>
    /// Using the perimeters stored in PerimeterData, creates Area2Ds describing the floor.
    /// </summary>
    /// <param name="perimData">Contains info on all perimeters.</param>
    /// <param name="tileMaps">List of all tilemaps.</param>
    private void _CreateTilemapArea2Ds(PerimeterData perimData, TileMapList tileMaps)
    {
        foreach (TileMap tileMap in tileMaps.Values)
        {
            var area2D = new Area2D();
            area2D.Name = tileMap.Name + AREA_NAME;
            tileMap.AddChild(area2D);

            int maxTileGroups = perimData.GetMaxTileGroup(tileMap);
            for (int tileGroup = 0; tileGroup < maxTileGroups; tileGroup++)
            {
                int maxHoleGroups = perimData.GetMaxHoleGroup(tileMap, tileGroup);
                EdgeCollection[] allPerims = new EdgeCollection[maxHoleGroups];
                
                for (int holeGroup = 0; holeGroup < maxHoleGroups; holeGroup++)
                {
                    allPerims[holeGroup] = perimData.GetEdgeCollection(tileMap, tileGroup, holeGroup);
                }

                //RECTANGLE DECOMPOSITION HERE

            }
        
        }
    }

    /// <summary>
    /// Using perimeters stored in PerimeterData, creates walls for each of them.
    /// Walls from perimeter P go on the tilemap LOWER than the tilemap that contains P.
    /// </summary>
    /// <param name="perimData"></param>
    /// <param name="tileMaps"></param>
    private void _CreateTilemapWalls(PerimeterData perimData, TileMapList tileMaps)
    {
        for (int i = 0; i < tileMaps.Count; i++)
        {
            TileMap tileMap = tileMaps[i];
            StaticBody2D walls = new StaticBody2D();
            walls.Name = tileMap.Name + Globals.STATIC_BODY_WALLS_NAME;
            tileMap.AddChild(walls);

            if (i == 0) continue; //no need to construct walls for floor

            int maxTileGroups = perimData.GetMaxTileGroup(tileMap);
            for (int tileGroup = 0; tileGroup < maxTileGroups; tileGroup++)
            {
                int maxHoleGroups = perimData.GetMaxHoleGroup(tileMap, tileGroup);
                for (int holeGroup = 0; holeGroup < maxHoleGroups; holeGroup++)
                {
                    EdgeCollection edgeCol = perimData.GetEdgeCollection(tileMap, tileGroup, holeGroup);

                    CollisionPolygon2D cp2d = this._BuildClosedWall(edgeCol);
                    cp2d = this._ShiftWallDown(cp2d);

                    TileMap targetTileMap = tileMaps[i - 1];
                    StaticBody2D targetWalls = (StaticBody2D)targetTileMap.FindNode(targetTileMap.Name + Globals.STATIC_BODY_WALLS_NAME, 
                                                                                    false, false);
                    if (targetWalls is object)
                    {
                        targetWalls.AddChild(cp2d);
                    }
                } //holeGroup for
            } //tileGroup for
        } //tileMap for
    }



    /////////////////////////
    /////////////////////////
    ///BUILDER FUNCS BELOW///
    /////////////////////////
    /////////////////////////

    /// <summary>
    /// Given an input collection of edges, builds CollisionPolygon2D walls.
    /// Assumes closed loop.
    /// </summary>
    /// <param name="edgeCol"></param>
    /// <returns></returns>
    private CollisionPolygon2D _BuildClosedWall(EdgeCollection edgeCol)
    {
        CollisionPolygon2D cp2d = new CollisionPolygon2D();
        cp2d.BuildMode = CollisionPolygon2D.BuildModeEnum.Segments;
        Vector2[] polygon = new Vector2[edgeCol.Count];

        for (int i = 0; i < edgeCol.Count; i++)
        {
            polygon[i] = edgeCol[i].a;
        }

        cp2d.Polygon = polygon;
        return cp2d;
    }

    /// <summary>
    /// Shifts CP2D polygon down by Globals.TILE_HEIGHT. Used for moving colliders for walls on a higher
    /// tilemap to a lower one so that they are in the right place.  
    /// I am forced to modify and return parameter CP2D because no cloning method exists. Forgive me object
    /// immutability gods.
    /// </summary>
    /// <param name="cp2d">CP2D that is being shifted down.</param>
    /// <returns>The same CP2D input, but shifted down.</returns>
    private CollisionPolygon2D _ShiftWallDown(CollisionPolygon2D cp2d)
    {
        Vector2[] polygon = cp2d.Polygon;
        for (int i = 0; i < polygon.Length; i++)
        {
            polygon[i] = polygon[i] + (new Vector2(0, Globals.TILE_HEIGHT));
        }
        cp2d.Polygon = polygon;
        return cp2d;
    }

}
}