using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace EFSMono.SystemModules.SplitSpritePaintingModule.SplitSpritePaintingObjects
{
    /// <summary>
    /// A class that contains a spritesheet and information taken from its accompanying JSON, as well as methods that
    /// allow for 'lookup' of:
    ///     1. Animation Name
    ///     2. Animation Frame
    /// ..to return its:
    ///     1. Split Sprite Positions
    ///     2. Split Sprite Sizes
    ///     3. Split Sprite Z Indexes
    ///     4. Sheet Position so we can actually draw the thing
    /// ..for each split sprite that exists.
    /// </summary>
    public class SplitSpriteSheet : Image
    {
        private readonly Dictionary<SplitSpriteSheetKey, List<SplitSpriteSheetValue>> _spriteFromAnim;
        private readonly Dictionary<string, int> _animFramesFromAnimName;
        private readonly Texture _spriteSheet;

        public SplitSpriteSheet(Texture spriteSheet, List<SplitSpriteInfo> spriteSheetInfo)
        {
            if (spriteSheetInfo == null) throw new ArgumentNullException(nameof(spriteSheetInfo), "Input list of spriteSheetInfo was null.");
            this._spriteFromAnim = new Dictionary<SplitSpriteSheetKey, List<SplitSpriteSheetValue>>();
            this._animFramesFromAnimName = new Dictionary<string, int>();
            this._spriteSheet = spriteSheet;

            this._FillSpriteSheetDict(spriteSheetInfo);

            foreach (SplitSpriteSheetKey key in this._spriteFromAnim.Keys)
            {
                List<SplitSpriteSheetValue> valueList = this._spriteFromAnim[key];
                GD.PrintS("val list size: " + valueList.Count);
                GD.PrintS("for key: " + key.ToString());
            }
        }

        /// <summary>
        /// Fill this class's dictionaries that:
        ///     1. maps animation parameters to a list of sprite parameters.
        ///     2. maps an animation name to its number of animation frames.
        /// </summary>
        /// <param name="spriteSheetInfo"></param>
        private void _FillSpriteSheetDict(List<SplitSpriteInfo> spriteSheetInfo)
        {
            foreach (SplitSpriteInfo info in spriteSheetInfo)
            {
                this._SetAnimMaxFrame(info.animName, info.animFrame);
                this._AddSplitSpriteInfo(info);
            }
        }

        /// <summary>
        /// Creates an entry in this class's dictionary that maps anim name to its number of frames if said anim name
        /// does not yet exist.  
        /// If the anim name does exist, sets its number of frames to the higher of the two between:
        ///     1. the existing frame number
        ///     2. the input frame number + 1
        /// </summary>
        /// <param name="animName"></param>
        /// <param name="animFrame"></param>
        private void _SetAnimMaxFrame(string animName, int animFrame)
        {
            if (!this._animFramesFromAnimName.ContainsKey(animName))
            {
                this._animFramesFromAnimName[animName] = animFrame + 1;
            }
            else
            {
                int currentMaxFrame = this._animFramesFromAnimName[animName];
                this._animFramesFromAnimName[animName] = (currentMaxFrame > animFrame + 1) ?
                                                               currentMaxFrame : animFrame + 1;
            }
        }

        /// <summary>
        /// Given the input <paramref name="info"/>, checks if a key made from its animation name + frame already exists
        /// within this class's dictionary that maps split sprite info to animation parameters.  
        /// If not, creates an entry for it and adds the split sprite info, otherwise just add the split sprite info
        /// to the existing entry.
        /// </summary>
        /// <param name="info"></param>
        private void _AddSplitSpriteInfo(SplitSpriteInfo info)
        {
            var key = new SplitSpriteSheetKey(info.animName, info.animFrame);
            var val = new SplitSpriteSheetValue(info.splitIndex, info.splitPosition, info.size, info.zIndex, info.sheetPosition);

            if (!this._spriteFromAnim.ContainsKey(key))
            {
                this._spriteFromAnim[key] = new List<SplitSpriteSheetValue>
                {
                    val
                };
            }
            else
            {
                this._spriteFromAnim[key].Add(val);
            }
        }

        /// <summary>
        /// Return this max num of frames associated with the input <paramref name="animName"/>.
        /// </summary>
        /// <param name="animName"></param>
        /// <returns></returns>
        public int GetMaxFrames(string animName)
        {
            return this._animFramesFromAnimName[animName];
        }

        /// <summary>
        /// Return the info of all split sprites associated with some given animation parameters, or an
        /// empty list IFF the input animation parameters do not exist.
        /// </summary>
        /// <param name="animName"></param>
        /// <param name="animFrame"></param>
        /// <returns></returns>
        public List<SplitSpriteSheetValue> GetSplitSpriteInfoForAnim(string animName, int animFrame)
        {
            var key = new SplitSpriteSheetKey(animName, animFrame);
            if (this._spriteFromAnim.ContainsKey(key))
            {
                return this._spriteFromAnim[key];
            }
            else
            {
                return new List<SplitSpriteSheetValue>();
            }
        }

        public List<SplitSpriteSheetValue> GetAnyInfo()
        {
            return this._spriteFromAnim.Values.First();
        }

        public RID GetSpriteSheetRID()
        {
            return this._spriteSheet.GetRid();
        }

        public void Unload()
        {
            this._spriteSheet.Dispose();
        }
    }
}