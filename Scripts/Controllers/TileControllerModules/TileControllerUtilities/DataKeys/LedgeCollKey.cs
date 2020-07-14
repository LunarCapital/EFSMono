using Godot;

namespace TileControllerNamespace
{
/// <summary>
/// A class used as a key for the Data class dictionaries.
/// This class specifically is used for mapping a ledge group to its edges,
/// represented as an EdgeCollection.
/// </summary>
public class LedgeCollKey
{
    private TileMap tileMap;
    private int tileGroup;
    private int holeGroup;
    private TileMap superTileMap;
    private int ledgeGroup;

    public LedgeCollKey(TileMap tileMap, int tileGroup, int holeGroup, TileMap superTileMap, int ledgeGroup)
    {
        this.tileMap = tileMap;
        this.tileGroup = tileGroup;
        this.holeGroup = holeGroup;
        this.superTileMap = superTileMap;
        this.ledgeGroup = ledgeGroup;
    }

    public override int GetHashCode()
    {
        return this.tileMap.GetHashCode() ^ 
               this.tileGroup.GetHashCode() ^
               this.holeGroup.GetHashCode() ^
               this.superTileMap.GetHashCode() ^
               this.ledgeGroup.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as LedgeCollKey);
    }

    public bool Equals(LedgeCollKey obj)
    {
        return this.tileMap == obj.tileMap &&
               this.tileGroup == obj.tileGroup &&
               this.holeGroup == obj.holeGroup &&
               this.superTileMap == obj.superTileMap &&
               this.ledgeGroup == obj.ledgeGroup;
    }

}
}