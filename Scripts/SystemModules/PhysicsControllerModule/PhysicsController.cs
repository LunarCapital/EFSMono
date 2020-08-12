using EFSMono.Scripts.SystemModules.GeneralUtilities;
using EFSMono.Scripts.SystemModules.PhysicsControllerModule.HubMessages;
using EFSMono.Scripts.SystemModules.PhysicsControllerModule.RIDConstructors;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Ledge;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter;
using Godot;
using TinyMessenger;
using System.Collections.Generic;
using System.Timers;
using EFSMono.Scripts.SystemModules.EntityTrackerModule.HubMessages;
using Timer = System.Timers.Timer;

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
    //Variables
    private readonly TinyMessengerHub _toEntityHub;
    
    private Dictionary<TileMap, RID> _tileMapToFloorArea2Ds;
    private Dictionary<TileMap, RID> _tileMapToWallSB2Ds;
    private Dictionary<TileMap, RID> _tileMapToLedgeSB2Ds;
    private Dictionary<RID, List<ConvexPolygonShape2D>> _floorArea2DToPolygons;
    private Dictionary<RID, List<SegmentShape2D>> _wallSB2DToSegments;
    private Dictionary<RID, List<SegmentShape2D>> _ledgeSB2DToSegments;

    public PhysicsController(TinyMessengerHub toEntityHub)
    {
        this._toEntityHub = toEntityHub;
        this._toEntityHub.Subscribe<EntityChangedZIndexMessage>(this._HandleEntityChangedZIndex);

        this._tileMapToFloorArea2Ds = new Dictionary<TileMap, RID>();
        this._tileMapToWallSB2Ds = new Dictionary<TileMap, RID>();
        this._tileMapToLedgeSB2Ds = new Dictionary<TileMap, RID>();
        this._floorArea2DToPolygons = new Dictionary<RID, List<ConvexPolygonShape2D>>();
        this._wallSB2DToSegments = new Dictionary<RID, List<SegmentShape2D>>();
        this._ledgeSB2DToSegments = new Dictionary<RID, List<SegmentShape2D>>();
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
        
        this._tileMapToFloorArea2Ds = tileMaps.ConstructTileMapFloorMap(worldSpace);
        this._tileMapToWallSB2Ds = tileMaps.ConstructTileMapCollisionMap(worldSpace);
        this._tileMapToLedgeSB2Ds = tileMaps.ConstructTileMapCollisionMap(worldSpace);
        this._floorArea2DToPolygons = tileMaps.ConstructFloorPartitions(this._tileMapToFloorArea2Ds, perimData);
        this._wallSB2DToSegments = tileMaps.ConstructSB2DSegments(this._tileMapToWallSB2Ds, perimData, ledgeData,
                                                          CollisionConstructor.StaticBodyOrigin.WALL);
        this._ledgeSB2DToSegments = tileMaps.ConstructSB2DSegments(this._tileMapToLedgeSB2Ds, perimData, ledgeData, 
                                                           CollisionConstructor.StaticBodyOrigin.LEDGE);
        
        this._toEntityHub.Publish(new FloorsConstructedMessage(this, this._tileMapToFloorArea2Ds));
        foreach (TileMap tileMap in tileMaps.Values)
        {
            //GD.PrintS("TileMapToFloorArea2Ds RID for tilemap with zIndex " + tileMap.ZIndex + ": " + this.tileMapToFloorArea2Ds[tileMap].GetId());
            //GD.PrintS("TileMapToWallSB2Ds RID for tilemap with zIndex " + tileMap.ZIndex + ": " +  this.tileMapToWallSB2Ds[tileMap].GetId());
            //GD.PrintS("TileMapToLedgeSB2Ds RID for tilemap with zIndex " + tileMap.ZIndex + ": " +  this.tileMapToLedgeSB2Ds[tileMap].GetId());

            RID floorRID = this._tileMapToFloorArea2Ds[tileMap];
            RID wallRID = this._tileMapToWallSB2Ds[tileMap];
            RID ledgeRID = this._tileMapToLedgeSB2Ds[tileMap];
            GD.PrintS("floorArea2DToPolygons count: " + this._floorArea2DToPolygons[floorRID].Count);
            GD.PrintS("wallSB2DToSegments count: " + this._wallSB2DToSegments[wallRID].Count);
            GD.PrintS("ledgeSB2DToSegments count: " + this._ledgeSB2DToSegments[ledgeRID].Count);
        }
    }
    
    /// <summary>
    /// Free all RIDs stored.
    /// </summary>
    private void _FreeRIDs()
    {
        var allRIDs = new List<RID>();
        allRIDs.AddRange(this._tileMapToFloorArea2Ds.Values);
        allRIDs.AddRange(this._tileMapToWallSB2Ds.Values);
        allRIDs.AddRange(this._tileMapToLedgeSB2Ds.Values);
        foreach (List<ConvexPolygonShape2D> polygons in this._floorArea2DToPolygons.Values)
        {
            allRIDs.AddRange(polygons.ConvertAll(x => x.GetRid()));
        }

        foreach (List<SegmentShape2D> segments in this._wallSB2DToSegments.Values)
        {
            allRIDs.AddRange(segments.ConvertAll(x => x.GetRid()));
        }

        foreach (List<SegmentShape2D> segments in this._ledgeSB2DToSegments.Values)
        {
            allRIDs.AddRange(segments.ConvertAll(x => x.GetRid()));
        }

        foreach (RID rid in allRIDs)
        {
            Physics2DServer.FreeRid(rid);
        }
    }

    /// <summary>
    /// Set all walls and ledges that are not on the same z index as some entity to ignore it.
    /// Passed from entity MessageHub subscription and is fired when EntityTracker detects that an entity has changed z index.
    /// </summary>
    /// <param name="msg"></param>
    private void _HandleEntityChangedZIndex(EntityChangedZIndexMessage msg)
    {
        RID entityRID = msg.GetEntityRID();
        int zIndex = msg.GetZIndex();

        foreach (TileMap tileMap in this._tileMapToWallSB2Ds.Keys)
        {
            if (tileMap.ZIndex == zIndex) Physics2DServer.BodyRemoveCollisionException(this._tileMapToWallSB2Ds[tileMap], entityRID);
            else Physics2DServer.BodyAddCollisionException(this._tileMapToWallSB2Ds[tileMap], entityRID);
        }
        foreach (TileMap tileMap in this._tileMapToLedgeSB2Ds.Keys)
        {
            if (tileMap.ZIndex == zIndex) Physics2DServer.BodyRemoveCollisionException(this._tileMapToLedgeSB2Ds[tileMap], entityRID);
            else Physics2DServer.BodyAddCollisionException(this._tileMapToLedgeSB2Ds[tileMap], entityRID);
        }
    }
}
}
