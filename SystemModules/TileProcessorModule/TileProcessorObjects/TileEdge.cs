using EFSMono.Common.Autoload;
using EFSMono.Common.DataStructures.Geometry;
using Godot;

namespace EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects
{
    /// <summary>
    /// A TileEdge class that extends Edge to specifically describe a tile edge.
    /// The purpose of this class is to be used as a helper for the TileController in building
    /// the Area2D and colliders for each TileMap
    /// </summary>
    public class TileEdge : Edge
    {
        public Vector2 tileCoords { get; } //coords of the tile that the edge came from
        public int tileSide { get; }

        public TileEdge(Vector2 a, Vector2 b, Vector2 tileCoords, int tileSide) : base(a, b)
        {
            this.tileCoords = tileCoords;
            this.tileSide = tileSide;
        }

        public override int GetHashCode()
        {
            int hashCode = this.tileCoords.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Gets the reversed edge of this edge, which is just a and b swapped
        /// </summary>
        /// <returns>An edge with the same properties as this one but with a and b swapped</returns>
        public override Edge GetReverseEdge()
        {
            return new TileEdge(this.b, this.a, this.tileCoords, this.tileSide)
            {
                isChecked = this.isChecked
            };
        }

        /// <summary>
        /// Shifts Edge up or down depending on input integer.
        /// For an input of n = 1, shifts Edge upwards by one TileMap.  To shift downards, make input negative.
        /// </summary>
        /// <param name="n">Modifier of how far the Edge is shifted.</param>
        /// <returns>A new edge with coordinates that have been shifted from this Edge's coordinates.</returns>
        public TileEdge GetShiftedByN(int n)
        {
            Vector2 shiftedA = this.a - (n * new Vector2(0, Globals.TileHeight));
            Vector2 shiftedB = this.b - (n * new Vector2(0, Globals.TileHeight));
            Vector2 shiftedTileCoords = this.tileCoords - (n * Vector2.One);

            return new TileEdge(shiftedA, shiftedB, shiftedTileCoords, this.tileSide);
        }

        /// <summary>
        /// Checks if this edge is identical to some comparison edge
        /// </summary>
        /// <param name="comparisonEdge"></param>
        /// <returns>True if identical, false if not</returns>
#pragma warning disable IDE0051 // Remove unused private members
        private bool _IsIdentical(TileEdge comparisonEdge)
#pragma warning restore IDE0051 // Remove unused private members
        {
            return (this.a == comparisonEdge.a && this.b == comparisonEdge.b) ||
                   (this.a == comparisonEdge.b && this.b == comparisonEdge.a);
        }
    }
}