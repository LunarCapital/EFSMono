using System;
using System.Collections.Generic;
using System.Linq;
using EFSMono.GameObjects;
using EFSMono.SystemModules.EntityBirthDeathModule;
using EFSMono.SystemModules.EntityTrackerModule.HubMessages;
using EFSMono.SystemModules.EntityTrackerModule.Objects;
using EFSMono.SystemModules.PhysicsControllerModule.HubMessages;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects;
using Godot;
using TinyMessenger;

namespace EFSMono.SystemModules.EntityTrackerModule
{
    /// <summary>
    /// A class that tracks entities and reacts to their state, etc by changing their TileMap parent
    /// when they jump/fall.
    /// </summary>
    public class EntityTracker : Node2D
    {
        //Variables
        private TinyMessengerHub _toPhysicsHub;
        private TinyMessageSubscriptionToken _floorConstructedToken;
        private TinyMessageSubscriptionToken _areaMonitorsSetupToken;
        private TinyMessageSubscriptionToken _area2DCallbackToken;

        private TileMapList _tileMaps;
        private ReadonlyEntityPackage _entityPackage;
        private Dictionary<int, EntityZPigeonhole> _entityIDToZPigeonhole;
        private bool _ready;
        private int _waitPhysicsSteps;

        public override void _PhysicsProcess(float delta)
        {
            if (this._waitPhysicsSteps > 0)
            {
                this._waitPhysicsSteps--;
                if (this._waitPhysicsSteps == 0) this._ready = true;
                GD.PrintS("physics step taken");
                return;
            }

            if (!this._ready) return;
            foreach (Entity entity in this._entityPackage.entitiesByID.Values)
            {
                if (entity.gravityComponent.inAir)
                {
                    if (entity.gravityComponent.raiseHeight)
                    {
                        this.CallDeferred(nameof(_MakeEntityRise), entity);
                    }
                    continue;
                }
                Node parent = entity.GetParent();
                if (parent is null) continue;

                if (this._entityIDToZPigeonhole[entity.GetRid().GetId()].IsZEmpty(((TileMap)parent).ZIndex - 1))
                {
                    this.CallDeferred(nameof(_MakeEntityFall), entity);
                }
                else
                {
                    entity.gravityComponent.SetGrounded();
                }
            }
        }

        public void ReceiveMsgHub(TinyMessengerHub toPhysicsHub)
        {
            this._toPhysicsHub = toPhysicsHub;
        }

        /// <summary>
        /// Load a world by setting this class to track its Entities.
        /// TODO figure out whether deferred-queue is enough to ensure this loads after every other world-reliant function is finished so nothing funny happens like we load a world before an Area2D exit callback finishes and crashes the game
        /// TODO if yes, PULL latch in this func to prevent async funcs and entities moving, AND have the async funcs use a stack of tokens or something that this func needs to wait for it to be empty before continuing
        /// </summary>
        /// <param name="tileMaps">A list of all TileMaps in the world.</param>
        public void LoadWorld(TileMapList tileMaps, ReadonlyEntityPackage entityPackage)
        {
            this._floorConstructedToken = this._toPhysicsHub.Subscribe<FloorsConstructedMessage>((msg) => this._HandleFloorsConstructed(msg));
            this._areaMonitorsSetupToken = this._toPhysicsHub.Subscribe<AreaMonitorsSetupMessage>((msg) => this._waitPhysicsSteps = 1);
            this._area2DCallbackToken = this._toPhysicsHub.Subscribe<Area2DCallbackMessage>((msg) => this._HandleArea2DCallback(msg));
            
            this._entityPackage = entityPackage;
            this._entityIDToZPigeonhole = new Dictionary<int, EntityZPigeonhole>();
            this._tileMaps = tileMaps ?? throw new ArgumentNullException(nameof(tileMaps), "Attempted to load world with no tilemaps.");
            this._ready = false;
            this._waitPhysicsSteps = 0;
        }

        /// <summary>
        /// Handle all the tasks that need to happen when an entity changes a layer, AKA it no longer collides with walls
        /// and ledges on different layers.
        /// </summary>
        /// <param name="entity">Entity that changed Z index.</param>
        /// <param name="zIndex">New Z index that <param>entity</param> is now on.</param>
        private void _HandleEntityZIndexChange(Entity entity, int zIndex)
        {
            this._toPhysicsHub.Publish(new EntityChangedZIndexMessage(this, entity.GetRid(), zIndex));
        }

        /// <summary>
        /// Connects floor Area2Ds to a callback function in this class.
        /// Passed from physics MessageHub subscription and is fired when PhysicsController finishes building floor Area2Ds.
        /// </summary>
        /// <param name="msg">Message received from TinyMessageHub notifying this class that floor Area2Ds have been created.</param>
        private void _HandleFloorsConstructed(FloorsConstructedMessage msg)
        {
            foreach (Entity entity in this._entityPackage.entitiesByID.Values)
            {
                this._HandleEntityZIndexChange(entity, ((TileMap)entity.GetParent()).ZIndex - 1);
                this._entityIDToZPigeonhole[entity.GetRid().GetId()] = new EntityZPigeonhole(entity.GetRid().GetId(), this._tileMaps.Count);
            }
            this._ready = true;
        }

        /// <summary>
        /// Keeps track of what shapes within Area2Ds each entity is touching.
        /// Subscribed to the event hub connected to PhysicsController.
        /// </summary>
        /// <param name="msg">Area2DCallbackMessage contaning info on what area was entered by what body.</param>
        private void _HandleArea2DCallback(Area2DCallbackMessage msg)
        {
            if (msg.areaBodyStatus == Physics2DServer.AreaBodyStatus.Added)
            {
                GD.PrintS("entity with id: " + msg.entityRID.GetId() + " entered area with id: " + msg.areaID + " via its shape with index: " + msg.areaShapeIndex + " on z level: " + msg.areaZIndex);
                this._entityIDToZPigeonhole[msg.entityRID.GetId()].EnteredAreaShape(msg.areaZIndex, msg.areaShapeIndex);
            }
            else
            {
                GD.PrintS("entity with id: " + msg.entityRID.GetId() + " exited area with id: " + msg.areaID + " via its shape with index: " + msg.areaShapeIndex + " on z level: " + msg.areaZIndex);
                this._entityIDToZPigeonhole[msg.entityRID.GetId()].ExitedAreaShape(msg.areaZIndex, msg.areaShapeIndex);
            }
        }

        private void _MakeEntityFall(Entity entity)
        {
            var currentTileMap = (TileMap)entity.GetParent();
            TileMap lowerTileMap = this._tileMaps[currentTileMap.ZIndex - 1];

            entity.gravityComponent.SetInAir();
            currentTileMap.RemoveChild(entity);
            lowerTileMap.AddChild(entity);
            this._HandleEntityZIndexChange(entity, lowerTileMap.ZIndex - 1);
        }

        private void _MakeEntityRise(Entity entity)
        {
            var currentTileMap = (TileMap)entity.GetParent();
            TileMap higherTileMap = this._tileMaps[currentTileMap.ZIndex + 1];

            entity.gravityComponent.SetAscendParameters();
            currentTileMap.RemoveChild(entity);
            higherTileMap.AddChild(entity);
            this._HandleEntityZIndexChange(entity, higherTileMap.ZIndex - 1);
        }
    }
}