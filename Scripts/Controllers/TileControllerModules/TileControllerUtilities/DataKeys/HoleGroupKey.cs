using Godot;

namespace TileControllerNamespace
{
/// <summary>
/// A class used as a key for the Data class dictionaries.
/// This class specifically is used for mapping a tile group to its number of hole groups.
/// </summary>
public class HoleGroupKey
{
    private TileMap tileMap;
    private int tileGroup;

    public HoleGroupKey(TileMap tileMap, int tileGroup)
    {
        this.tileMap = tileMap;
        this.tileGroup = tileGroup;
    }

    public override int GetHashCode()
    {
        return this.tileGroup.GetHashCode() ^ this.tileGroup.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as HoleGroupKey);
    }

    public bool Equals(HoleGroupKey obj)
    {
        return this.tileMap == obj.tileMap && this.tileGroup == obj.tileGroup;
    }

}
}