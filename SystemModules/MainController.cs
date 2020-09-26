using System;
using System.Collections.Generic;
using System.Linq;
using EFSMono.Common.Autoload;
using EFSMono.Preloading;
using EFSMono.SystemModules.EntityBirthDeathModule;
using EFSMono.SystemModules.EntityTrackerModule;
using EFSMono.SystemModules.PhysicsControllerModule;
using EFSMono.SystemModules.SplitSpritePaintingModule;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Ledge;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter;
using Godot;
using TinyMessenger;

namespace EFSMono.SystemModules
{
    public class MainController : Node
    {
        private EntityBirthDeathHandler _entityBirthDeathHandler;
        private EntityTracker _entityTracker;
        private PhysicsController _physicsControl;
        private SplitSpritePainter _splitSpritePainter;

        public override void _Ready()
        {
            this._PreloadResources();

            var controllerNode = (Node2D)this.GetNode((Globals.ControllerNodeName));
            var physicsToEntityHub = new TinyMessengerHub();

            this._entityBirthDeathHandler = new EntityBirthDeathHandler();
            this._entityTracker = (EntityTracker)controllerNode.FindNode(Globals.EntityTrackerNodeName, false);
            this._physicsControl = new PhysicsController(physicsToEntityHub);
            this._splitSpritePainter = (SplitSpritePainter)controllerNode.FindNode(Globals.SplitSpritePainterNodeName, false);

            this._entityTracker.ReceiveMsgHub(physicsToEntityHub);

            /*var watch = System.Diagnostics.Stopwatch.StartNew();
            this._LoadWorld();
            watch.Stop();
            GD.PrintS("world load took: " + watch.ElapsedMilliseconds + " ms.");*/
        }

        private void _PreloadResources()
        {
            Preloader.Preload();
        }

        public void SwitchWorld(PackedScene packedWorld)
        {
            if (packedWorld is null) throw new ArgumentNullException(nameof(packedWorld));
            this._UnloadWorld();
            var newWorld = (Node2D)packedWorld.Instance();
            this.AddChild(newWorld);
            this._LoadWorld();
        }

        private void _UnloadWorld()
        {
            if (this.HasNode(Globals.WorldNodeName))
            {
                GD.PrintS("has world");
            }
            else if (this.HasNode("Menu"))
            {
                GD.PrintS("has title");
            }

            this._physicsControl.UnloadWorld();
        }

        private void _LoadWorld()
        {
            var worldNode = (Node2D)this.GetNode(Globals.WorldNodeName);
            var children = new List<Node>(worldNode.GetChildren().OfType<Node>());
            (TileMapList tileMaps, PerimeterData perimData, LedgeData ledgeData) = TileProcessorModule.TileProcessor.BuildTileNodes(children);

            this._entityBirthDeathHandler.LoadWorld(tileMaps);
            ReadonlyEntityPackage entityPackage = this._entityBirthDeathHandler.GetEntityPackage();

            this._entityTracker.LoadWorld(tileMaps, entityPackage);
            this._splitSpritePainter.LoadWorld(tileMaps, entityPackage);
            this._physicsControl.LoadWorld(tileMaps, perimData, ledgeData, worldNode.GetWorld2d().Space);
        }
    }
}