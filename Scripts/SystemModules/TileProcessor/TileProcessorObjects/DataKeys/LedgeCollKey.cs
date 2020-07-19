using Godot;

namespace TileProcessorNamespace
{
/// <summary>
/// A class used as a key for the Data class dictionaries.
/// This class specifically is used for mapping a ledge group to its edges,
/// represented as an EdgeCollection.
/// </summary>
public class LedgeCollKey
{
    private TileMap _tileMap { get; }
    private int _tileGroup { get; }
    private int _holeGroup { get; }
    private TileMap _superTileMap { get; }
    private int _ledgeGroup { get; }

    public LedgeCollKey(TileMap tileMap, int tileGroup, int holeGroup, TileMap superTileMap, int ledgeGroup)
    {
        this._tileMap = tileMap;
        this._tileGroup = tileGroup;
        this._holeGroup = holeGroup;
        this._superTileMap = superTileMap;
        this._ledgeGroup = ledgeGroup;
    }

    public override int GetHashCode()
    {
        return this._tileMap.GetHashCode() ^
               this._tileGroup.GetHashCode() ^
               this._holeGroup.GetHashCode() ^
               this._superTileMap.GetHashCode() ^
               this._ledgeGroup.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as LedgeCollKey);
    }

    public bool Equals(LedgeCollKey obj)
    {
        return this._tileMap == obj._tileMap &&
               this._tileGroup == obj._tileGroup &&
               this._holeGroup == obj._holeGroup &&
               this._superTileMap == obj._superTileMap &&
               this._ledgeGroup == obj._ledgeGroup;
    }

}
}