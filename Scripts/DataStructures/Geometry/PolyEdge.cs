using EFSMono.Scripts.Autoload;
using Godot;

namespace EFSMono.Scripts.DataStructures.Geometry
{
/// <summary>
/// An Edge class that describes an edge of polygons.
/// The purpose of this class is to be used for segment simplification for PhysicsControllers
/// floor partitioning.
/// </summary>
public class PolyEdge : IEdge
{
    public Vector2 a { get; }
    public Vector2 b { get; }
    public bool isChecked { get; set; }

    public PolyEdge(Vector2 a, Vector2 b)
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

    IEdge IEdge.GetReverseEdge()
    {
        return this.GetReverseEdge();
    }

    public bool IsIdentical(IEdge other)
    {
        return this.IsIdentical((PolyEdge) other);
    }

    public PolyEdge Clone()
    {
        return (PolyEdge)this.MemberwiseClone();
    }
    
    /// <summary>
    /// Gets the reversed edge of this edge, which is just a and b swapped
    /// </summary>
    /// <returns>An edge with the same properties as this one but with a and b swapped</returns>
    private PolyEdge GetReverseEdge()
    {
        return new PolyEdge(this.b, this.a)
        {
            isChecked = this.isChecked
        };
    }

    /// <summary>
    /// Checks if this edge is identical to some comparison edge
    /// </summary>
    /// <param name="comparisonEdge"></param>
    /// <returns>True if identical, false if not</returns>
    private bool IsIdentical(PolyEdge comparisonEdge)
    {
        return (this.a == comparisonEdge.a && this.b == comparisonEdge.b) ||
               (this.a == comparisonEdge.b && this.b == comparisonEdge.a);
    }
}
}