using Godot;

namespace TileProcessorNamespace
{
/// <summary>
/// A class used as a key for the Data class dictionaries.
/// This class specifically is used for mapping a tile group to its number of hole groups.
/// </summary>
public class HoleGroupKey
{
    private TileMap _tileMap { get; }
    private int _tileGroup { get; }

    public HoleGroupKey(TileMap tileMap, int tileGroup)
    {
        this._tileMap = tileMap;
        this._tileGroup = tileGroup;
    }

    public override int GetHashCode()
    {
        return this._tileGroup.GetHashCode() ^ this._tileGroup.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as HoleGroupKey);
    }

    public bool Equals(HoleGroupKey obj)
    {
        return this._tileMap == obj._tileMap && this._tileGroup == obj._tileGroup;
    }

}
}