using EFSMono.Scripts.Autoload;
using Godot;

namespace EFSMono.Scripts.Tools
{
[Tool]
public class TileMapOffset : TileMap {
    public override void _Ready() {
        var navPoly = new NavigationPolygon();
        var offset = new Vector2(0, - Globals.TILE_HEIGHT);

        foreach (int tileID in this.TileSet.GetTilesIds()) {
            this.TileSet.TileSetTextureOffset(tileID, offset);

            if (tileID == 0) {
                navPoly = this.TileSet.TileGetNavigationPolygon(tileID);
            } else {
                this.TileSet.TileSetNavigationPolygon(tileID, navPoly);
            }
        }

    }
}
}
