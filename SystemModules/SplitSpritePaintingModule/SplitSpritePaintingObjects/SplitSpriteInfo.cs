using Godot;

namespace EFSMono.SystemModules.SplitSpritePaintingModule.SplitSpritePaintingObjects
{
    /// <summary>
    /// A class with all the necessary variables required to store information about an isometrically split spritesheet's JSON.
    /// </summary>
    public class SplitSpriteInfo
    {
        public Vector2 sheetPosition { get; set; }
        public Vector2 size { get; set; }
        public int splitIndex { get; set; }
        public Vector2 splitPosition { get; set; }
        public int zIndex { get; set; }
        public string animName { get; set; }
        public int animFrame { get; set; }
    }
}