using EFSMono.Scripts.SystemModules.GeneralUtilities;
using EFSMono.Scripts.SystemModules.PhysicsControllerModule.HubMessages;
using EFSMono.Scripts.SystemModules.PhysicsControllerModule.RIDConstructors;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Ledge;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter;
using Godot;
using TinyMessenger;
using SCol = System.Collections.Generic;

namespace EFSMono.Scripts.SystemModules.PhysicsControllerModule {
/// <summary>
/// A class made to interface with a PhysicsServer2D for a number of purposes that will probably
/// need to be put into their own modules:
///     1. Building RIDs for all walls
///     2. Building RIDs for all ledges
///     3. Switching wall/ledge collision exceptions whenever entities change Z index
/// </summary>
public class PhysicsController
{
    //Wall StaticBodies of tilemaps
    //Ledge StaticBodies of tilemaps
    //arrays of RIDs of collision objects of each wall StaticBody
    //arrays of RIDs of collision objects of each ledge StaticBody
    //Array of RIDs of entities????
        //may put this in a separate one called EntityController or something?

    //Variables
    private readonly TinyMessengerHub _toEntityHub;
    
    public SCol.Dictionary<TileMap, RID> tileMapToFloorArea2Ds;
    public SCol.Dictionary<TileMap, RID> tileMapToWallSB2Ds;
    public SCol.Dictionary<TileMap, RID> tileMapToLedgeSB2Ds;
    public SCol.Dictionary<RID, SCol.List<ConvexPolygonShape2D>> floorArea2DToPolygons;
    public SCol.Dictionary<RID, SCol.List<SegmentShape2D>> wallSB2DToSegments;
    public SCol.Dictionary<RID, SCol.List<SegmentShape2D>> ledgeSB2DToSegments;
    //TODO evaluate whether it is required that these are public and set to private if they are not

    //Delegates
    //public delegate void FloorsConstructedEventHandler(object sender, FloorsConstructedEventArgs args);
    //Events
    //public event System.EventHandler<FloorsConstructedEventArgs> FloorsConstructed;

    public PhysicsController(TinyMessengerHub toEntityHub)
    {
        this._toEntityHub = toEntityHub;
        this.tileMapToFloorArea2Ds = new SCol.Dictionary<TileMap, RID>();
        this.tileMapToWallSB2Ds = new SCol.Dictionary<TileMap, RID>();
        this.tileMapToLedgeSB2Ds = new SCol.Dictionary<TileMap, RID>();
        this.floorArea2DToPolygons = new SCol.Dictionary<RID, SCol.List<ConvexPolygonShape2D>>();
        this.wallSB2DToSegments = new SCol.Dictionary<RID, SCol.List<SegmentShape2D>>();
        this.ledgeSB2DToSegments = new SCol.Dictionary<RID, SCol.List<SegmentShape2D>>();
    }

    /// <summary>
    /// Should be called whenever loaded world changes.
    /// Resets all wall and ledge physics bodies and prepares new ones to be filled by TileController.
    /// </summary>
    /// <param name="tileMaps">List of TileMaps which are guaranteed to have a unique Z index.</param>
    /// <param name="perimData">Data of all perimeters of all TileMaps.</param>
    /// <param name="ledgeData">Data of all ledges of all TileMaps.</param>
    /// <param name="worldSpace">Current world space.</param>
    public void LoadWorld(TileMapList tileMaps, PerimeterData perimData, LedgeData ledgeData, RID worldSpace)
    {
        this._FreeRIDs();
        
        this.tileMapToFloorArea2Ds = tileMaps.ConstructTileMapFloorMap(worldSpace);
        this.tileMapToWallSB2Ds = tileMaps.ConstructTileMapCollisionMap(worldSpace);
        this.tileMapToLedgeSB2Ds = tileMaps.ConstructTileMapCollisionMap(worldSpace);
        this.floorArea2DToPolygons = tileMaps.ConstructFloorPartitions(this.tileMapToFloorArea2Ds, perimData);
        this.wallSB2DToSegments = tileMaps.ConstructSB2DSegments(this.tileMapToWallSB2Ds, perimData, ledgeData,
                                                          CollisionConstructor.StaticBodyOrigin.WALL);
        this.ledgeSB2DToSegments = tileMaps.ConstructSB2DSegments(this.tileMapToLedgeSB2Ds, perimData, ledgeData, 
                                                           CollisionConstructor.StaticBodyOrigin.LEDGE);
        
        this._toEntityHub.Publish(new FloorsConstructedMessage(this, this.tileMapToFloorArea2Ds));
    }

    /// <summary>
    /// Free all RIDs stored.
    /// </summary>
    private void _FreeRIDs()
    {
        var allRIDs = new SCol.List<RID>();
        allRIDs.AddRange(this.tileMapToFloorArea2Ds.Values);
        allRIDs.AddRange(this.tileMapToWallSB2Ds.Values);
        allRIDs.AddRange(this.tileMapToLedgeSB2Ds.Values);
        foreach (SCol.List<ConvexPolygonShape2D> polygons in this.floorArea2DToPolygons.Values)
        {
            allRIDs.AddRange(polygons.ConvertAll(x => x.GetRid()));
        }

        foreach (SCol.List<SegmentShape2D> segments in this.wallSB2DToSegments.Values)
        {
            allRIDs.AddRange(segments.ConvertAll(x => x.GetRid()));
        }

        foreach (SCol.List<SegmentShape2D> segments in this.ledgeSB2DToSegments.Values)
        {
            allRIDs.AddRange(segments.ConvertAll(x => x.GetRid()));
        }

        foreach (RID rid in allRIDs)
        {
            Physics2DServer.FreeRid(rid);
        }
    }

}
}
