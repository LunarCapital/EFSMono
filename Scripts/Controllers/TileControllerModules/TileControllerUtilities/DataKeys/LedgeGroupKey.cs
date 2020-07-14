using Godot;

namespace TileControllerNamespace
{
/// <summary>
/// A class used as a key for the Data class dictionaries.
/// This class specifically is used for mapping a superimposed TileMap to its number of ledge groups.
/// </summary>
public class LedgeGroupKey
{
    private TileMap tileMap;
    private int tileGroup;
    private int holeGroup;
    private TileMap superTileMap;
    
    public LedgeGroupKey(TileMap tileMap, int tileGroup, int holeGroup, TileMap superTileMap)
    {
        this.tileMap = tileMap;
        this.tileGroup = tileGroup;
        this.holeGroup = holeGroup;
        this.superTileMap = superTileMap;
    }

    public override int GetHashCode()
    {
        return this.tileMap.GetHashCode() ^ 
               this.tileGroup.GetHashCode() ^ 
               this.holeGroup.GetHashCode() ^
               this.superTileMap.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as LedgeGroupKey);
    }

    public bool Equals(LedgeGroupKey obj)
    {
        return this.tileMap == obj.tileMap &&
               this.tileGroup == obj.tileGroup &&
               this.holeGroup == obj.holeGroup &&
               this.superTileMap == obj.superTileMap;
    }

}
}