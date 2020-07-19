using Godot;

namespace TileProcessorNamespace
{
/// <summary>
/// A class used as a key for the Data class dictionaries.
/// This class specifically is used for mapping a superimposed TileMap to its number of ledge groups.
/// </summary>
public class LedgeGroupKey
{
    private TileMap _tileMap { get; }
    private int _tileGroup { get; }
    private int _holeGroup { get; }
    private TileMap _superTileMap { get; }

    public LedgeGroupKey(TileMap tileMap, int tileGroup, int holeGroup, TileMap superTileMap)
    {
        this._tileMap = tileMap;
        this._tileGroup = tileGroup;
        this._holeGroup = holeGroup;
        this._superTileMap = superTileMap;
    }

    public override int GetHashCode()
    {
        return this._tileMap.GetHashCode() ^
               this._tileGroup.GetHashCode() ^
               this._holeGroup.GetHashCode() ^
               this._superTileMap.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as LedgeGroupKey);
    }

    public bool Equals(LedgeGroupKey obj)
    {
        return this._tileMap == obj._tileMap &&
               this._tileGroup == obj._tileGroup &&
               this._holeGroup == obj._holeGroup &&
               this._superTileMap == obj._superTileMap;
    }

}
}