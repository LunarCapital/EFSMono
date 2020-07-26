using Godot;
// ReSharper disable ClassNeverInstantiated.Global

namespace EFSMono.Scripts.Autoload
{
/// <summary>
/// Auto loaded script.
/// Contains easily accessible global constants.
/// </summary>
public class Globals : Node
{
    //NODE NAMES CONSTANTS
    public const string WORLD_NODE_NAME = "World";
    public const string CONTROLLER_NODE_NAME = "Controllers";
    public const string ENTITY_TRACKER_NODE_NAME = "EntityTracker";
    
    //TILE PARAMS CONSTANTS
    public const int TILE_WIDTH = 64;
    public const int TILE_HEIGHT = 32;
    public enum Side {NORTH = 0, EAST = 1, SOUTH = 2, WEST = 3}; //North is ALWAYS top-right.
    public static readonly Vector2 NORTH_VEC2 = new Vector2(0, -1);
    public static readonly Vector2 EAST_VEC2 = new Vector2(1, 0);
    public static readonly Vector2 SOUTH_VEC2 = new Vector2(0, 1);
    public static readonly Vector2 WEST_VEC2 = new Vector2(-1, 0);
}
}
