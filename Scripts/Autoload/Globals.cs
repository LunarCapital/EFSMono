using Godot;
using System;

namespace AutoloadNamespace
{
/// <summary>
/// Autoloaded script.
/// Contains easily accessible global constants.
/// </summary>
public class Globals : Node
{

    //TILE PARAMS CONSTANTS
    public const int TILE_WIDTH = 64;
    public const int TILE_HEIGHT = 32;
    public enum SIDE : int {NORTH = 0, EAST = 1, SOUTH = 2, WEST = 3}; //North is ALWAYS top-right.
    public static readonly Vector2 NORTH_VEC2 = new Vector2(0, -1);
    public static readonly Vector2 EAST_VEC2 = new Vector2(1, 0);
    public static readonly Vector2 SOUTH_VEC2 = new Vector2(0, 1);
    public static readonly Vector2 WEST_VEC2 = new Vector2(-1, 0);

    //TILE NAMING CONSTANTS
    public const string STATIC_BODY_LEDGES_NAME = "Ledges";
    public const string STATIC_BODY_WALLS_NAME = "Walls";
}
}
