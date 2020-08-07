using Godot;

namespace EFSMono.Scripts.DataStructures.Geometry
{
/// <summary>
///Abstract class Edge, which describes a segment between two coordinates.
/// </summary>
public abstract class Edge
{
    public Vector2 a { get; }
    public Vector2 b { get; }
    public bool isChecked { get; set; }

    protected Edge(Vector2 a, Vector2 b)
    {
        this.a = a;
        this.b = b;
        this.isChecked = false;
    }

    public abstract Edge GetReverseEdge();
    // {
    //     return new PolyEdge(this.b, this.a)
    //     {
    //         isChecked = this.isChecked
    //     };   
    // }
    
    public bool IsIdentical(Edge other)
    {
        return (this.a == other.a && this.b == other.b) ||
               (this.a == other.b && this.b == other.a);
    }
}
}