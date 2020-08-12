using Godot;

namespace EFSMono.Scripts.DataStructures.Geometry
{
/// <summary>
/// A class that represents a polygon chord, AKA a line between two concave vertices.
/// </summary>
public class Chord
{
    public enum Direction {VERTICAL = 0, HORIZONTAL = 1};

    public Vector2 a { get; }
    public Vector2 b { get; }
    public readonly Direction direction;

    public Chord(Vector2 a, Vector2 b)
    {
        this.a = a;
        this.b = b;
        this.direction = this.a.x == this.b.x ? Direction.VERTICAL : Direction.HORIZONTAL;
    }
}
}