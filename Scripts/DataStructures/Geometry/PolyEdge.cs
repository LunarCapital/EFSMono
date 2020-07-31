using EFSMono.Scripts.Autoload;
using Godot;

namespace EFSMono.Scripts.DataStructures.Geometry
{
/// <summary>
/// An Edge class that describes an edge of polygons.
/// The purpose of this class is to be used for segment simplification for PhysicsControllers
/// floor partitioning.
/// </summary>
public class PolyEdge : Edge
{
    public Vector2 a { get; }
    public Vector2 b { get; }
    public bool isChecked { get; set; }

    public PolyEdge(Vector2 a, Vector2 b) : base(a, b)
    {
        this.a = a;
        this.b = b;
        this.isChecked = false;
    }

    public override int GetHashCode()
    {
        int hashCode = this.a.GetHashCode() ^ this.b.GetHashCode();
        return hashCode;
    }

    public PolyEdge Clone()
    {
        return (PolyEdge)this.MemberwiseClone();
    }
    
    /// <summary>
    /// Gets the reversed edge of this edge, which is just a and b swapped
    /// </summary>
    /// <returns>An edge with the same properties as this one but with a and b swapped</returns>
    private new PolyEdge GetReverseEdge()
    {
        return new PolyEdge(this.b, this.a)
        {
            isChecked = this.isChecked
        };
    }
}
}