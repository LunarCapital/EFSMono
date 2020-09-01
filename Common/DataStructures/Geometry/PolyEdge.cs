using Godot;

namespace EFSMono.Common.DataStructures.Geometry
{
    /// <summary>
    /// An Edge class that describes an edge of polygons.
    /// The purpose of this class is to be used for segment simplification for PhysicsControllers
    /// floor partitioning.
    /// </summary>
    public class PolyEdge : Edge
    {
        public PolyEdge(Vector2 a, Vector2 b) : base(a, b)
        {
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
        public override Edge GetReverseEdge()
        {
            return new PolyEdge(this.b, this.a)
            {
                isChecked = this.isChecked
            };
        }
    }
}