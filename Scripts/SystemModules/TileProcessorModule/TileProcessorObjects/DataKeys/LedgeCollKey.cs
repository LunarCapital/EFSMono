using Godot;

namespace EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.DataKeys
{
/// <summary>
/// A class used as a key for the Data class dictionaries.
/// This class specifically is used for mapping a ledge group to its edges,
/// represented as an EdgeCollection.
/// </summary>
public class LedgeCollKey
{
    private readonly TileMap _tileMap;
    private readonly int _tileGroup;
    private readonly int _holeGroup;
    private readonly TileMap _superTileMap;
    private readonly int _ledgeGroup;

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

    private bool Equals(LedgeCollKey obj)
    {
        return this._tileMap == obj._tileMap &&
               this._tileGroup == obj._tileGroup &&
               this._holeGroup == obj._holeGroup &&
               this._superTileMap == obj._superTileMap &&
               this._ledgeGroup == obj._ledgeGroup;
    }

}
}