using System.Linq;
using EFSMono.Scripts.Entities;
using EFSMono.Scripts.SystemModules.GeneralUtilities;
using EFSMono.Scripts.SystemModules.PhysicsControllerModule.HubMessages;
using Godot;
using TinyMessenger;
using SCol = System.Collections.Generic;
// ReSharper disable ClassNeverInstantiated.Global

namespace EFSMono.Scripts.SystemModules.EntityTrackerModule
{
/// <summary>
/// A class that tracks entities and reacts to their state, etc by changing their TileMap parent
/// when they jump/fall.
/// </summary>
public class EntityTracker : Node2D
{
    //Variables
    private TinyMessengerHub _toPhysicsHub;
    //TODO a MessengerHub that tracks entity creation/deletion, DAMN I love this TinyMessenger shit 100% gonna overuse and regret it
    private SCol.List<Entity> _entities;

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
    public void LoadWorld(TileMapList tileMaps)
    {        
        this._toPhysicsHub.Subscribe<FloorsConstructedMessage>((msg) => 
                                                               this._HandleFloorsConstructed(tileMaps, msg));
        this._entities = new SCol.List<Entity>();
        foreach (TileMap tileMap in tileMaps.Values)
        {
            foreach (Entity entity in tileMap.GetChildren().OfType<Entity>())
            {
                this._entities.Add(entity);
            }
        }
    }
    
    /// <summary>
    /// Connects floor Area2Ds to a callback function in this class. 
    /// Passed to physics MessageHub subscription and is fired when PhysicsController finishes building floor Area2Ds.
    /// </summary>
    /// <param name="tileMaps">A list of all TileMaps.</param>
    /// <param name="msg">Message received from TinyMessageHub notifying this class that floor Area2Ds have been created.</param>
    private void _HandleFloorsConstructed(TileMapList tileMaps, FloorsConstructedMessage msg)
    {
        foreach (TileMap tileMap in tileMaps.Values)
        {
            RID area2dRID = msg.GetRidFromTilemap(tileMap);
            Physics2DServer.AreaSetMonitorCallback(area2dRID, this, nameof(this._OnAreaCallback));
        }
    }

    /// <summary>
    /// Keeps track of what Area2Ds each entity is touching.
    /// Fires when an Area2D reports that a PhysicsBody has entered or exited one of its shapes.
    /// </summary>
    /// <param name="bodyStatus">Whether body entered or exited area.</param>
    /// <param name="bodyRID">RID of body that interacted with area.</param>
    /// <param name="bodyInstanceID">ID of instance of body that interacted with area.</param>
    /// <param name="bodyShapeID">ID of body shape that interacted with area.</param>
    /// <param name="areaShapeID">ID of area shape that was exited/entered.</param>
    private void _OnAreaCallback(Physics2DServer.AreaBodyStatus bodyStatus, RID bodyRID, int bodyInstanceID, 
                                 int bodyShapeID, int areaShapeID)
    {
        
    }
}
}