using Godot;
using System.Linq;
using SCol = System.Collections.Generic;
using TPN = TileProcessorNamespace;
using MCN = MainControllerNamespace;

namespace PhysicsControllerNamespace {
/// <summary>
/// A class made to interface with a PhysicsServer2D for a number of purposes that will probably
/// need to be put into their own modules:
///     1. Building RIDs for all walls
///     2. Building RIDs for all ledges
///     3. Switching wall/ledge collision exceptions whenever entities change Z index
/// </summary>
public class PhysicsController
{
    //Wall staticbodies of tilemaps
    //Ledge staticbodies of tilemaps
    //arrays of RIDs of collision objects of each wall staticbody
    //ararys of RIDs of collision objects of each ledge staticbody
    //Array of RIDs of entities????
        //may put this in a separate one called EntityController or something?

    //Variables
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
    public event System.EventHandler<FloorsConstructedEventArgs> FloorsConstructed;

    public PhysicsController()
    {
        tileMapToFloorArea2Ds = new SCol.Dictionary<TileMap, RID>();
        tileMapToWallSB2Ds = new SCol.Dictionary<TileMap, RID>();
        tileMapToLedgeSB2Ds = new SCol.Dictionary<TileMap, RID>();
        floorArea2DToPolygons = new SCol.Dictionary<RID, SCol.List<ConvexPolygonShape2D>>();
        wallSB2DToSegments = new SCol.Dictionary<RID, SCol.List<SegmentShape2D>>();
        ledgeSB2DToSegments = new SCol.Dictionary<RID, SCol.List<SegmentShape2D>>();
    }

    /// <summary>
    /// Should be called whenever loaded world changes.
    /// Resets all wall and ledgephysics bodies and prepares new ones to be filled by TileController.
    /// </summary>
    /// <param name="tileMaps">List of TileMaps which are guaranteed to have a unique Z index.</param>
    public void LoadWorld(MCN.TileMapList tileMaps, TPN.PerimeterData perimData, TPN.LedgeData ledgeData, RID worldSpace)
    {
        this._FreeRIDs();

        CollisionConstructor collisionConstructor = new CollisionConstructor();
        tileMapToFloorArea2Ds = collisionConstructor.ConstructTileMapFloorMap(tileMaps, worldSpace);
        tileMapToWallSB2Ds = collisionConstructor.ConstructTileMapCollisionMap(tileMaps, worldSpace);
        tileMapToLedgeSB2Ds = collisionConstructor.ConstructTileMapCollisionMap(tileMaps, worldSpace);
        wallSB2DToSegments = collisionConstructor.ConstructSB2DSegments(tileMaps, tileMapToWallSB2Ds, perimData,
                                                                        ledgeData, CollisionConstructor.STATIC_BODY_ORIGIN.WALL);
        ledgeSB2DToSegments = collisionConstructor.ConstructSB2DSegments(tileMaps, tileMapToLedgeSB2Ds, perimData,
                                                                         ledgeData, CollisionConstructor.STATIC_BODY_ORIGIN.LEDGE);

        //Delegate/Event to send tileMapToFloorArea2Ds to EntityTracker for callback setting?
        this._OnFloorsConstructed(new FloorsConstructedEventArgs(tileMapToFloorArea2Ds));
    }

    /// <summary>
    /// Free all RIDs stored.
    /// </summary>
    private void _FreeRIDs()
    {
        SCol.List<RID> allRIDs = new SCol.List<RID>();
        allRIDs.AddRange(tileMapToFloorArea2Ds?.Values);
        allRIDs.AddRange(tileMapToWallSB2Ds?.Values);
        allRIDs.AddRange(tileMapToLedgeSB2Ds?.Values);
        foreach (SCol.List<ConvexPolygonShape2D> polygons in floorArea2DToPolygons.Values)
        {
            allRIDs.AddRange(polygons.ConvertAll(x => x.GetRid()));
        }
        foreach (SCol.List<SegmentShape2D> segments in wallSB2DToSegments.Values)
        {
            allRIDs.AddRange(segments.ConvertAll(x => x.GetRid()));
        }
        foreach (SCol.List<SegmentShape2D> segments in ledgeSB2DToSegments.Values)
        {
            allRIDs.AddRange(segments.ConvertAll(x => x.GetRid()));
        }

        foreach (RID rid in allRIDs)
        {
            Physics2DServer.FreeRid(rid);
        }
    }

    /// <summary>
    /// Publishes an event stating that this class has constructed the floor Area2Ds for some loaded
    /// world.
    /// Protected and virtual out of convention, doubt I'll ever extend this.
    /// </summary>
    /// <param name="args"></param>
    protected virtual void _OnFloorsConstructed(FloorsConstructedEventArgs args)
    {
            FloorsConstructed?.Invoke(this, args);
    }

}
}
