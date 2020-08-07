using Godot;
using TinyMessenger;
using System.Collections.Generic;

namespace EFSMono.Scripts.SystemModules.PhysicsControllerModule.HubMessages
{
/// <summary>
/// An EventArgs class for PhysicsController containing the Physics2DServer-initialised
/// RIDs for floor Area2Ds, which will be passed to an event listener in EntityTracker
/// so it can connect signals from each Area2D to itself for entering/exiting.
/// </summary>
public class FloorsConstructedMessage : ITinyMessage
{
    public object Sender { get; private set; }
    private readonly Dictionary<TileMap, RID> _tileMapToFloorArea2Ds;

    public FloorsConstructedMessage(object sender, Dictionary<TileMap, RID> tileMapToFloorArea2Ds)
    {
        this.Sender = sender;
        this._tileMapToFloorArea2Ds = tileMapToFloorArea2Ds;
    }

    public RID GetRidFromTilemap(TileMap tileMap)
    {
        if (this._tileMapToFloorArea2Ds.ContainsKey(tileMap))
        {
            return this._tileMapToFloorArea2Ds[tileMap];
        }
        else
        {
            throw new KeyNotFoundException("Dict " + nameof(this._tileMapToFloorArea2Ds) + " does not contain key TileMap " + nameof(tileMap));
        }
    }
}
}