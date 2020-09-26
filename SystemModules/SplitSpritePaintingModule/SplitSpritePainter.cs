using System;
using System.Collections.Generic;
using System.Linq;
using EFSMono.GameObjects;
using EFSMono.SystemModules.EntityBirthDeathModule;
using EFSMono.SystemModules.SplitSpritePaintingModule.Helpers;
using EFSMono.SystemModules.SplitSpritePaintingModule.SplitSpritePaintingObjects;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects;
using Godot;

namespace EFSMono.SystemModules.SplitSpritePaintingModule
{
    /// <summary>
    /// A class that uses godot's VisualServer to draw sprites for entities.
    /// "But hang on, can't entities just hold a Sprite node as a child?"
    /// Yes, but thanks to Isometric 2D, there exists a problem where any entity that is bigger than a tile will always be
    /// YSorted incorrectly.  Note to myself: If you ever forget what this looks like, there's an image on the google doc.
    ///
    /// As such, every entity's sprite needs to be split into divisions (most likely 64x64) at some set intervals (likely 16
    /// pixels) and rendered SEPARATELY so that they are YSorted separately and therefore correctly.  Additionally, each
    /// division has its own ZIndex just in case an entity's sprite has different height levels (think of a tall entity that
    /// spans two tile-heights, its top half would always be drawn UNDER any tiles on a higher Z level, even if the entity
    /// is standing in front of them).
    /// </summary>
    public class SplitSpritePainter : Node2D
	{
        private TileMapList _tileMaps;
        private ReadonlyEntityPackage _entityPackage;
        private bool _ready;

        private Dictionary<int, EntityDrawPackage> _entityIDToDrawPackage;

        public override void _PhysicsProcess(float delta)
        {
            if (!_ready) return;
            foreach (Entity entity in this._entityPackage.entitiesByID.Values)
            {
                if (entity.GetParent() is null) continue;
                int tileMapZIndex = ((TileMap)entity.GetParent()).ZIndex;
                if (this._entityIDToDrawPackage.TryGetValue(entity.GetRid().GetId(), out EntityDrawPackage drawPackage))
                {
                    drawPackage.FreeDrawnRIDs();
                    drawPackage.DrawSplitSprites(this._tileMaps, tileMapZIndex, entity.Position, entity.gravityComponent.spriteDrawHeightPos);
                }
            }
        }

		public void LoadWorld(TileMapList tileMaps, ReadonlyEntityPackage entityPackage)
        {
            this._ready = false;
            this._tileMaps = tileMaps ?? throw new ArgumentNullException(nameof(tileMaps), "Attempted to load world with no tilemaps.");
            this._entityPackage = entityPackage;
            this._entityIDToDrawPackage = new Dictionary<int, EntityDrawPackage>();

			foreach (Entity entity in this._entityPackage.entitiesByID.Values)
			{
				string spritePath = entity.fullSprite.Texture.ResourcePath;
				string spritePathPrefix = spritePath.Substr(0, spritePath.FindLast("."));
                Texture spriteSheet = spritePathPrefix.OpenSplitSpriteImage();
                GD.PrintS("new img sprite sheet rid: " + spriteSheet.GetRid().GetId());
				List<SplitSpriteInfo> spriteSheetInfo = spritePathPrefix.OpenSplitSpriteJson();

                int entityID = entity.GetRid().GetId();
                GD.PrintS("id: " + entityID);
				var splitSpriteSheet = new SplitSpriteSheet(spriteSheet, spriteSheetInfo);
                GD.PrintS(splitSpriteSheet.GetAnyInfo().First().size);
                var idToSplitNodes = new Dictionary<int, Node2D>();
                List<SplitSpriteSheetValue> splitInfo = splitSpriteSheet.GetAnyInfo();
                foreach (SplitSpriteSheetValue info in splitInfo)
                {
                    var splitNode2D = new Node2D();
                    idToSplitNodes[info.splitIndex] = splitNode2D;
                }
                this._entityIDToDrawPackage.Add(entityID, new EntityDrawPackage(entityID, splitSpriteSheet, idToSplitNodes, entity.fullSprite.Offset));
                entity.fullSprite.Hide();
			}
            this._ready = true;
		}

        public void UnloadWorld()
        {
            foreach (EntityDrawPackage drawPackage in this._entityIDToDrawPackage.Values)
            {
                drawPackage.Unload();
            }
        }
	}
}
