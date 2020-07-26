using EFSMono.Scripts.Autoload;
using Godot;

namespace EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects
{
/// <summary>
/// An Edge class that describes an 'edge' of something like a polygon.
/// The purpose of this class is to be used as a helper for the TileController in building
/// the Area2D and colliders for each TileMap.
/// </summary>
public class Edge
{
    //Variables
    public Vector2 a { get; }
    public Vector2 b { get; }
    public Vector2 tileCoords { get; } //coords of the tile that the edge came from
    public int tileSide { get; }
    public bool isChecked { get; set; }

    public Edge(Vector2 a, Vector2 b, Vector2 tileCoords, int tileSide)
    {
        this.a = a;
        this.b = b;
        this.tileCoords = tileCoords;
        this.tileSide = tileSide;
        this.isChecked = false;
    }

    public override int GetHashCode()
    {
        int hashCode = this.tileCoords.GetHashCode();
        return hashCode;
    }

    public Edge Clone()
    {
        return (Edge)this.MemberwiseClone();
    }

    /// <summary>
    /// Gets the reversed edge of this edge, which is just a and b swapped
    /// </summary>
    /// <returns>An edge with the same properties as this one but with a and b swapped</returns>
    public Edge GetReverseEdge()
    {
            return new Edge(this.b, this.a, this.tileCoords, this.tileSide)
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
    public Edge GetShiftedByN(int n)
    {
        Vector2 shiftedA = this.a - (n * new Vector2(0, Globals.TILE_HEIGHT));
        Vector2 shiftedB = this.b - (n * new Vector2(0, Globals.TILE_HEIGHT));
        Vector2 shiftedTileCoords = this.tileCoords - (n * Vector2.One);

        return new Edge(shiftedA, shiftedB, shiftedTileCoords, this.tileSide);
    }

    /// <summary>
    /// Checks if this edge is identical to some comparison edge
    /// </summary>
    /// <param name="comparisonEdge"></param>
    /// <returns>True if identical, false if not</returns>
    public bool IsIdentical(Edge comparisonEdge)
    {
        return (this.a == comparisonEdge.a && this.b == comparisonEdge.b) ||
               (this.a == comparisonEdge.b && this.b == comparisonEdge.a);
    }
}
}