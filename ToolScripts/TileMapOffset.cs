using EFSMono.Common.Autoload;
using Godot;

namespace EFSMono.ToolScripts
{
    [Tool]
    public class TileMapOffset : TileMap
    {
        public override void _Ready()
        {
            var navPoly = new NavigationPolygon();
            var offset = new Vector2(0, -Globals.TileHeight);

            foreach (int tileID in this.TileSet.GetTilesIds())
            {
                this.TileSet.TileSetTextureOffset(tileID, offset);

                if (tileID == 0)
                {
                    navPoly = this.TileSet.TileGetNavigationPolygon(tileID);
                }
                else
                {
                    this.TileSet.TileSetNavigationPolygon(tileID, navPoly);
                }
            }
        }
    }
}