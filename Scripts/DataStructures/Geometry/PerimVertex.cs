using Godot;

namespace EFSMono.Scripts.DataStructures.Geometry
{
/// <summary>
/// A class that holds a Vector2 representing a vertex, as well as an ID indicating which perimeter it originated from.
/// </summary>
public class PerimVertexUnused
{ //TODO delete this class if no longer needed
    public Vector2 vertex { get; }
    public int origin { get;  }

    public PerimVertexUnused(Vector2 vertex, int origin)
    {
        this.vertex = vertex;
        this.origin = origin;
    }

    public override int GetHashCode()
    {
        return this.vertex.GetHashCode() ^ this.origin.GetHashCode();
    }
/*
    public override bool Equals(object obj)
    {
        return this.Equals((PerimVertex) obj);
    }

    private bool Equals(PerimVertex other)
    {
        return this.vertex == other.vertex && this.origin == other.origin;
    }

    public bool IsVertexIdentical(PerimVertex other)
    {
        return this.vertex == other.vertex;
    }

    public bool IsOriginSame(PerimVertex other)
    {
        return this.origin == other.origin;
    }*/
}
}