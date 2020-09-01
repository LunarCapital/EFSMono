using System;
using System.Collections.Generic;
using EFSMono.SystemModules.EntityTrackerModule.HubMessages;
using EFSMono.SystemModules.PhysicsControllerModule.HubMessages;
using EFSMono.SystemModules.PhysicsControllerModule.Monitors;
using EFSMono.SystemModules.PhysicsControllerModule.RIDConstructors;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Ledge;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter;
using Godot;
using TinyMessenger;

namespace EFSMono.SystemModules.PhysicsControllerModule
{
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

        private readonly TinyMessageSubscriptionToken _entityChangedZIndexToken;

        private Dictionary<TileMap, RID> _tileMapToFloorArea2Ds;
        private Dictionary<TileMap, RID> _tileMapToWallSB2Ds;
        private Dictionary<TileMap, RID> _tileMapToLedgeSB2Ds;
        private Dictionary<RID, List<ConvexPolygonShape2D>> _floorArea2DToPolygons;
        private Dictionary<RID, List<SegmentShape2D>> _wallSB2DToSegments;
        private Dictionary<RID, List<SegmentShape2D>> _ledgeSB2DToSegments;

        private Dictionary<int, Area2DMonitor> _area2DMonitors;

        public PhysicsController(TinyMessengerHub toEntityHub)
        {
            this._toEntityHub = toEntityHub;
            this._entityChangedZIndexToken = this._toEntityHub.Subscribe<EntityChangedZIndexMessage>(this._HandleEntityChangedZIndex);

            this._tileMapToFloorArea2Ds = new Dictionary<TileMap, RID>();
            this._tileMapToWallSB2Ds = new Dictionary<TileMap, RID>();
            this._tileMapToLedgeSB2Ds = new Dictionary<TileMap, RID>();
            this._floorArea2DToPolygons = new Dictionary<RID, List<ConvexPolygonShape2D>>();
            this._wallSB2DToSegments = new Dictionary<RID, List<SegmentShape2D>>();
            this._ledgeSB2DToSegments = new Dictionary<RID, List<SegmentShape2D>>();

            this._area2DMonitors = new Dictionary<int, Area2DMonitor>();
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
            if (tileMaps == null) throw new ArgumentNullException(nameof(tileMaps), "Attempted to load world with no tilemaps.");
            
            this._tileMapToFloorArea2Ds = tileMaps.ConstructTileMapFloorMap(worldSpace);
            this._tileMapToWallSB2Ds = tileMaps.ConstructTileMapCollisionMap(worldSpace);
            this._tileMapToLedgeSB2Ds = tileMaps.ConstructTileMapCollisionMap(worldSpace);
            this._floorArea2DToPolygons = tileMaps.ConstructFloorPartitions(this._tileMapToFloorArea2Ds, perimData);
            this._wallSB2DToSegments = tileMaps.ConstructSB2DSegments(this._tileMapToWallSB2Ds, perimData, ledgeData,
                                                              CollisionConstructor.StaticBodyOrigin.Wall);
            this._ledgeSB2DToSegments = tileMaps.ConstructSB2DSegments(this._tileMapToLedgeSB2Ds, perimData, ledgeData,
                                                               CollisionConstructor.StaticBodyOrigin.Ledge);

            this._toEntityHub.Publish(new FloorsConstructedMessage(this, this._tileMapToFloorArea2Ds));
            foreach (TileMap tileMap in tileMaps.Values)
            {
                GD.PrintS("TileMapToFloorArea2Ds RID for tilemap with zIndex " + tileMap.ZIndex + ": " + this._tileMapToFloorArea2Ds[tileMap].GetId());
                //GD.PrintS("TileMapToWallSB2Ds RID for tilemap with zIndex " + tileMap.ZIndex + ": " +  this.tileMapToWallSB2Ds[tileMap].GetId());
                //GD.PrintS("TileMapToLedgeSB2Ds RID for tilemap with zIndex " + tileMap.ZIndex + ": " +  this.tileMapToLedgeSB2Ds[tileMap].GetId());

                RID floorRID = this._tileMapToFloorArea2Ds[tileMap];
                RID wallRID = this._tileMapToWallSB2Ds[tileMap];
                RID ledgeRID = this._tileMapToLedgeSB2Ds[tileMap];
                GD.PrintS("floorArea2DToPolygons count: " + this._floorArea2DToPolygons[floorRID].Count);
                GD.PrintS("wallSB2DToSegments count: " + this._wallSB2DToSegments[wallRID].Count);
                GD.PrintS("ledgeSB2DToSegments count: " + this._ledgeSB2DToSegments[ledgeRID].Count);

                this._area2DMonitors[floorRID.GetId()] = new Area2DMonitor(floorRID, tileMap.ZIndex, this._floorArea2DToPolygons[floorRID], this._toEntityHub);
                Physics2DServer.AreaSetMonitorCallback(floorRID, this._area2DMonitors[floorRID.GetId()], nameof(Area2DMonitor.OnAreaCallback));
            }
            this._toEntityHub.Publish(new AreaMonitorsSetupMessage(this));
            GD.PrintS("published areamonitor msg");
        }

        public void UnloadWorld()
        {
            this._FreeRIDs();
            if (!(this._entityChangedZIndexToken is null)) {
                this._toEntityHub.Unsubscribe(_entityChangedZIndexToken);
                this._entityChangedZIndexToken.Dispose();
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
            GD.PrintS("requested that entity with id: " + entityRID.GetId() + " be moved to collide with segments on z index: " + zIndex);

            foreach (TileMap tileMap in this._tileMapToWallSB2Ds.Keys)
            {
                if (tileMap.ZIndex == zIndex) Physics2DServer.BodyRemoveCollisionException(this._tileMapToWallSB2Ds[tileMap], entityRID);
                else Physics2DServer.BodyAddCollisionException(this._tileMapToWallSB2Ds[tileMap], entityRID);
            }
            foreach (TileMap tileMap in this._tileMapToLedgeSB2Ds.Keys)
            {
                if (tileMap.ZIndex == zIndex)
                {
                    Physics2DServer.BodyRemoveCollisionException(this._tileMapToLedgeSB2Ds[tileMap], entityRID);
                    GD.PrintS("removed collision with sb2d with id: " + this._tileMapToLedgeSB2Ds[tileMap].GetId());
                }
                else Physics2DServer.BodyAddCollisionException(this._tileMapToLedgeSB2Ds[tileMap], entityRID);
            }
        }
    }
}