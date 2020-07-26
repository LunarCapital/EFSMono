using Godot;

namespace EFSMono.Scripts.SystemModules.TileProcessorModule.TileProcessorObjects.DataKeys
{
/// <summary>
/// A class used as a key for the Data class dictionaries.
/// This class specifically is used for mapping a superimposed TileMap to its number of ledge groups.
/// </summary>
public class LedgeGroupKey
{
    private readonly TileMap _tileMap;
    private readonly int _tileGroup;
    private readonly int _holeGroup;
    private readonly TileMap _superTileMap;

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

    private bool Equals(LedgeGroupKey obj)
    {
        return this._tileMap == obj._tileMap &&
               this._tileGroup == obj._tileGroup &&
               this._holeGroup == obj._holeGroup &&
               this._superTileMap == obj._superTileMap;
    }

}
}