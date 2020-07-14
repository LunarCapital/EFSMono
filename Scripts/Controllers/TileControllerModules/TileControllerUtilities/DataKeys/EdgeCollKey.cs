using Godot;

namespace TileControllerNamespace
{
/// <summary>
/// A class used as a key for the Data class dictionaries.
/// This class specifically is used for mapping a hole group to its perimeter,
/// represented as an EdgeCollection.
/// </summary>
public class EdgeCollKey
{
    private TileMap tileMap;
    private int tileGroup;
    private int holeGroup;

    public EdgeCollKey(TileMap tileMap, int tileGroup, int holeGroup)
    {
        this.tileMap = tileMap;
        this.tileGroup = tileGroup;
        this.holeGroup = holeGroup;
    }

    public override int GetHashCode()
    {
        return this.tileMap.GetHashCode() ^
               this.tileGroup.GetHashCode() ^
               this.holeGroup.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as EdgeCollKey);
    }

    public bool Equals(EdgeCollKey obj)
    {
        return this.tileMap == obj.tileMap &&
               this.tileGroup == obj.tileGroup &&
               this.holeGroup == obj.holeGroup;
    }

}
}