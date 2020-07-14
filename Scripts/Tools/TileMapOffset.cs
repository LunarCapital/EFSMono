using Godot;
using System;
[Tool]

public class TileMapOffset : TileMap {

    public override void _Ready() {
        NavigationPolygon navPoly = new NavigationPolygon();
        var offset = new Vector2(0, -32);

        foreach (int tile_id in this.TileSet.GetTilesIds()) {
            this.TileSet.TileSetTextureOffset(tile_id, offset);

            if (tile_id == 0) {
                navPoly = this.TileSet.TileGetNavigationPolygon(tile_id);
            } else {
                this.TileSet.TileSetNavigationPolygon(tile_id, navPoly);
            }
        }

    }

}
