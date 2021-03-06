using Godot;

namespace EFSMono.Common.Autoload
{
    /// <summary>
    /// Auto loaded script.
    /// Contains easily accessible tile-related functions.
    /// </summary>
    public class TileFuncs : Node
    {
        /// <summary>
        /// Given some input tile, its layer, and an observation layer, calculate and return the coordinates
        /// for a tile that would theoretically be directly above or below the input tile on the observed layer.
        /// </summary>
        /// <param name="currentTile">Input tile below the tile that this function calcs coords for.</param>
        /// <param name="currentLayer">Layer of input tile.</param>
        /// <param name="observedLayer">Layer of the tile that this function calcs coords for.</param>
        /// <returns>Coords of tile on observed layer above input tile, even if said tile does not exist.</returns>
        public static Vector2 GetTileAboveOrBelow(Vector2 currentTile, int currentLayer, int observedLayer)
        {
            Vector2 offset = (currentLayer - observedLayer) * Vector2.One;
            Vector2 aboveTile = currentTile + offset;
            return aboveTile;
        }

        /// <summary>
        /// Given some current tile coordinates and a direction, calculate and return the coordinates
        /// for a tile that would theoretically be adjacent to the input tile in the input direction.
        /// Directions follow Globals.SIDE.
        /// If the direction input is invalid, returns the input coordinates.
        /// </summary>
        /// <param name="currentTile">Coordinates of current tile.</param>
        /// <param name="direction">Direction that this function uses to calc adj tile coords.</param>
        /// <returns>Coordinate of adj tile in the input direction from the input tile, even if said tile
        /// does not exist. If the input direction is invalid, returns the input tile coords.</returns>
        public static Vector2 GetTileAdjacent(Vector2 currentTile, int direction)
        {
            Vector2 adjTile = currentTile;

            switch (direction)
            {
                case (int)Globals.CWSide.North:
                    adjTile = currentTile + Globals.NorthVec2;
                    break;

                case (int)Globals.CWSide.East:
                    adjTile = currentTile + Globals.EastVec2;
                    break;

                case (int)Globals.CWSide.South:
                    adjTile = currentTile + Globals.SouthVec2;
                    break;

                case (int)Globals.CWSide.West:
                    adjTile = currentTile + Globals.WestVec2;
                    break;
            }
            return adjTile;
        }
    }
}