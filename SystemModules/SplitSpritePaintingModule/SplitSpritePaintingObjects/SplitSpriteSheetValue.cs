using Godot;

namespace EFSMono.SystemModules.SplitSpritePaintingModule.SplitSpritePaintingObjects
{
    public class SplitSpriteSheetValue
    {
        public int splitIndex { get; }
        public Vector2 splitPos { get; }
        public Vector2 size { get; }
        public int zIndex { get; }
        public Vector2 sheetPos { get; }

        public SplitSpriteSheetValue(int splitIndex, Vector2 splitPos, Vector2 size, int zIndex, Vector2 sheetPos)
        {
            this.splitIndex = splitIndex;
            this.splitPos = splitPos;
            this.size = size;
            this.zIndex = zIndex;
            this.sheetPos = sheetPos;
        }
    }
}
