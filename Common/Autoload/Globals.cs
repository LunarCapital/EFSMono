using Godot;

namespace EFSMono.Common.Autoload
{
    /// <summary>
    /// Auto loaded script.
    /// Contains easily accessible global constants.
    /// </summary>
    public class Globals : Node
    {
        //NODE NAMES CONSTANTS
        public const string WorldNodeName = "World";

        public const string ControllerNodeName = "Controllers";
        public const string EntityTrackerNodeName = "EntityTracker";
        public const string SplitSpritePainterNodeName = "SplitSpritePainter";

        //TILE PARAMS CONSTANTS
        public const int TileWidth = 64;

        public const int TileHeight = 32;

        public enum CWSide { North = 0, East = 1, South = 2, West = 3 }; //North is ALWAYS top-right.

        public enum CCWSide { North = 0, West = 1, South = 2, East = 3 };

        public static readonly Vector2 NorthVec2 = new Vector2(0, -1);
        public static readonly Vector2 EastVec2 = new Vector2(1, 0);
        public static readonly Vector2 SouthVec2 = new Vector2(0, 1);
        public static readonly Vector2 WestVec2 = new Vector2(-1, 0);

        public static int GetCWVectorValue(Vector2 vector)
        {
            int value;
            if (vector == NorthVec2) value = (int)CWSide.North;
            else if (vector == EastVec2) value = (int)CWSide.East;
            else if (vector == SouthVec2) value = (int)CWSide.South;
            else if (vector == WestVec2) value = (int)CWSide.West;
            else value = -1;
            return value;
        }

        public static int GetCCWVectorValue(Vector2 vector)
        {
            int value;
            if (vector == NorthVec2) value = (int)CCWSide.North;
            else if (vector == WestVec2) value = (int)CCWSide.West;
            else if (vector == SouthVec2) value = (int)CCWSide.South;
            else if (vector == EastVec2) value = (int)CCWSide.East;
            else value = -1;
            return value;
        }
    }
}