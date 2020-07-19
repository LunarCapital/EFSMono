using Godot;
using System;
using SCol = System.Collections.Generic;

namespace PhysicsControllerNamespace
{
/// <summary>
/// An EventArgs class for PhysicsController containing the Physics2DServer-initialised
/// RIDs for floor Area2Ds, which will be passed to an event listener in EntityTracker
/// so it can connect signals from each Area2D to itself for entering/exiting.
/// </summary>
public class FloorsConstructedEventArgs : EventArgs
{
    private readonly SCol.Dictionary<TileMap, RID> tileMapToFloorArea2Ds;

    public FloorsConstructedEventArgs(SCol.Dictionary<TileMap, RID> tileMapToFloorArea2Ds)
    {
        this.tileMapToFloorArea2Ds = tileMapToFloorArea2Ds;
    }

    public RID GetRIDFromTilemap(TileMap tileMap)
    {
        if (tileMapToFloorArea2Ds.ContainsKey(tileMap))
        {
            return tileMapToFloorArea2Ds[tileMap];
        }
        else
        {
            throw new SCol.KeyNotFoundException("Dict " + nameof(tileMapToFloorArea2Ds) + " does not contan key TileMap " + nameof(tileMap));
        }
    }
}
}