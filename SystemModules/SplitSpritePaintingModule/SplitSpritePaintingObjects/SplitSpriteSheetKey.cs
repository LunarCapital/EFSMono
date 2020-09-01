namespace EFSMono.SystemModules.SplitSpritePaintingModule.SplitSpritePaintingObjects
{
    public class SplitSpriteSheetKey
    {
        private readonly string _animName;
        private readonly int _animFrame;

        public SplitSpriteSheetKey(string animName, int animFrame)
        {
            this._animName = animName;
            this._animFrame = animFrame;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            return this._Equals((SplitSpriteSheetKey) obj);
        }

        public override int GetHashCode()
        {
            return this._animName.GetHashCode() ^ this._animFrame.GetHashCode();
        }

        public override string ToString()
        {
            return this._animName + ", " + this._animFrame;
        }
        private bool _Equals(SplitSpriteSheetKey other)
        {
            return this._animFrame == other._animFrame && this._animName == other._animName;
        }
    }
}