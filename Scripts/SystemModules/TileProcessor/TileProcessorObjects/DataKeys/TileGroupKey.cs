using Godot;

namespace TileProcessorNamespace
{
/// <summary>
/// A class used as a key for the Data class dictionaries.
/// This class specifically is used for mapping a TileMap to its number of tile groups.
/// </summary>
public class TileGroupKey
{
    private readonly TileMap tileMap;

    public TileGroupKey(TileMap tileMap)
    {
        this.tileMap = tileMap;
    }

    public override int GetHashCode()
    {
        return this.tileMap.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as TileGroupKey);
    }

    public bool Equals(TileGroupKey obj)
    {
        return this.tileMap == obj.tileMap;
    }

}
}