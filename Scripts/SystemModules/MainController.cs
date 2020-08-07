using System.Linq;
using EFSMono.Scripts.Autoload;
using EFSMono.Scripts.SystemModules.EntityTrackerModule;
using EFSMono.Scripts.SystemModules.GeneralUtilities;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Ledge;
using EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.Perimeter;
using TinyMessenger;
using Godot;
using System.Collections.Generic;

// ReSharper disable UnusedType.Global

namespace EFSMono.Scripts.SystemModules
{
    public class MainController : Node
{
    public override void _Ready()
    {
        var controllerNode = (Node2D) this.GetNode((Globals.CONTROLLER_NODE_NAME));
        var physicsToEntityHub = new TinyMessengerHub();
        var entityTracker = (EntityTracker) controllerNode.FindNode(Globals.ENTITY_TRACKER_NODE_NAME, false);
        var physicsControl = new PhysicsControllerModule.PhysicsController(physicsToEntityHub);
        entityTracker.ReceiveMsgHub(physicsToEntityHub);
        this._LoadWorld(entityTracker, physicsControl);
    }

    private void _LoadWorld(EntityTracker entityTracker, PhysicsControllerModule.PhysicsController physicsControl)
    {
        var worldNode = (Node2D)this.GetNode(Globals.WORLD_NODE_NAME);
        var children = new List<Node>(worldNode.GetChildren().OfType<Node>());
        (TileMapList tileMaps, PerimeterData perimData, LedgeData ledgeData) = TileProcessorModule.TileProcessor.BuildTileNodes(children);

        entityTracker.LoadWorld(tileMaps);
        physicsControl.LoadWorld(tileMaps, perimData, ledgeData, worldNode.GetWorld2d().Space);
    }
}
}