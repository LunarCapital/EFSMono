using Godot;

namespace TileProcessorNamespace
{
/// <summary>
/// A class used as a key for the Data class dictionaries.
/// This class specifically is used for mapping a hole group to its perimeter,
/// represented as an EdgeCollection.
/// </summary>
public class EdgeCollKey
{
    private TileMap _tileMap { get; }
    private int _tileGroup { get; }
    private int _holeGroup {get; }

    public EdgeCollKey(TileMap tileMap, int tileGroup, int holeGroup)
    {
        this._tileMap = tileMap;
        this._tileGroup = tileGroup;
        this._holeGroup = holeGroup;
    }

    public override int GetHashCode()
    {
        return this._tileMap.GetHashCode() ^
               this._tileGroup.GetHashCode() ^
               this._holeGroup.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as EdgeCollKey);
    }

    public bool Equals(EdgeCollKey obj)
    {
        return this._tileMap == obj._tileMap && //i know these are private and it makes me worried
               this._tileGroup == obj._tileGroup &&
               this._holeGroup == obj._holeGroup;
    }
    //To myself in the future, if you figured out if it's a bad idea for classes of the same type
    //to be able to read (but not set, I know that's bad) each others' private fields for the sake of
    //something like an Equals func, please bring me some peace of mind.

}
}