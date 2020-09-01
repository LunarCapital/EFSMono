using System.Collections.Generic;
using EFSMono.Common.Autoload;
using EFSMono.SystemModules.SplitSpritePaintingModule.SplitSpritePaintingObjects;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects;
using Godot;

namespace EFSMono.SystemModules.SplitSpritePaintingModule.Helpers
{
    /// <summary>
    /// A class that packages all the data needed to draw an Entity's split sprites into one place.
    /// </summary>
    class EntityDrawPackage
    {
#pragma warning disable IDE0052 // Remove unread private members
        private readonly int _entityID;
#pragma warning restore IDE0052 // Remove unread private members
        private readonly SplitSpriteSheet _splitSpriteSheet;
        private readonly Dictionary<int, Node2D> _idToSplitNodes;
        private readonly List<RID> _drawnSprites;
        private readonly Vector2 _relativePos;

        public EntityDrawPackage(int entityID, SplitSpriteSheet splitSpriteSheet, Dictionary<int, Node2D> idToSplitNodes, Vector2 relativePos)
        {
            this._entityID = entityID;
            this._splitSpriteSheet = splitSpriteSheet;
            this._idToSplitNodes = idToSplitNodes;
            this._drawnSprites = new List<RID>();
            this._relativePos = relativePos;
        }

        /// <summary>
        /// Attempts to free any split sprites drawn by the VisualServer related to this class's entity.
        /// </summary>
        public void FreeDrawnRIDs()
        {
            foreach (RID splitSpriteRID in this._drawnSprites)
            {
                VisualServer.FreeRid(splitSpriteRID);
            }
            this._drawnSprites.Clear();
        }

        /// <summary>
        /// Uses the VisualServer to draw each of an entity's split sprites, by:
        ///     1. Obtaining the Node2D that is designated to play host to a given split sprite
        ///     2. Changing the Node2D's (TileMap) parent if required
        ///     3. Creating an RID for the canvas item (the sprite drawing) and storing it so it can be freed later
        /// </summary>
        /// <param name="tileMaps"></param>
        /// <param name="tileMapZIndex"></param>
        /// <param name="entityPos"></param>
        /// <param name="entityDrawPos">Offsets the sprite upwards. Used for drawing entities falling/jumping.</param>
        public void DrawSplitSprites(TileMapList tileMaps, int tileMapZIndex, Vector2 entityPos, float entityDrawPos/*State goes here*/)
        {
            List<SplitSpriteSheetValue> splitSprites = this._splitSpriteSheet.GetSplitSpriteInfoForAnim("idle", 0); //TODO match to entity anim state

            foreach (SplitSpriteSheetValue splitSprite in splitSprites)
            {
                Node2D splitNode2D = this._idToSplitNodes[splitSprite.splitIndex];
                TileMap tileMap = tileMaps[tileMapZIndex + splitSprite.zIndex];
                if (splitNode2D.GetParent() != tileMap)
                {
                    if (splitNode2D.GetParent() != null) splitNode2D.GetParent().RemoveChild(splitNode2D);
                    tileMap.AddChild(splitNode2D);
                }

                RID splitSpriteRID = VisualServer.CanvasItemCreate();
                this._drawnSprites.Add(splitSpriteRID);
                VisualServer.CanvasItemSetParent(splitSpriteRID, splitNode2D.GetCanvasItem());

                VisualServer.CanvasItemAddTextureRectRegion(splitSpriteRID,
                    new Rect2(Vector2.Zero, splitSprite.size),
                    this._splitSpriteSheet.GetSpriteSheetRID(),
                    new Rect2(splitSprite.sheetPos, splitSprite.size),
                    new Color(1, 1, 1, 1),
                    false,
                    this._splitSpriteSheet.GetSpriteSheetRID());

                VisualServer.CanvasItemSetTransform(splitSpriteRID, new Transform2D(0, this._relativePos - new Vector2(Globals.TileWidth / 2, Globals.TileHeight / 2) + new Vector2(0, entityDrawPos)));

                splitNode2D.Position = entityPos + splitSprite.splitPos + new Vector2(Globals.TileWidth / 2, Globals.TileHeight / 2);
            }
        }

        public void Unload()
        {
            this._splitSpriteSheet.Unload();
        }
    }
}
