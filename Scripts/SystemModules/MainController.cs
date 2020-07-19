using Godot;
using System.Linq;
using SCol = System.Collections.Generic;
using TPN = TileProcessorNamespace;
using PCN = PhysicsControllerNamespace;

namespace MainControllerNamespace
{
public class MainController : Node
{
    public override void _Ready()
    {
        this._LoadWorld();
    }

    private void _LoadWorld()
    {
        TPN.TileProcessor tileProcess = new TPN.TileProcessor();
        Node2D world = (Node2D)GetNode("World");
        var children = new SCol.List<Node>(world.GetChildren().OfType<Node>());
        (TileMapList tileMaps, TPN.PerimeterData perimData, TPN.LedgeData ledgeData) = tileProcess.BuildTileNodes(children);

        PCN.PhysicsController physicsControl = new PCN.PhysicsController();
        physicsControl.LoadWorld(tileMaps, perimData, ledgeData, world.GetWorld2d().Space);
    }
}
}