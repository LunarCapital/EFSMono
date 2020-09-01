using System.Collections.Generic;
using System.IO;
using System.Linq;
using EFSMono.Common.Extensions;
using EFSMono.SystemModules.SplitSpritePaintingModule.SplitSpritePaintingObjects;
using Godot;
using Newtonsoft.Json.Linq;

namespace EFSMono.SystemModules.SplitSpritePaintingModule.Helpers
{
    /// <summary>
    /// A class that reads in the split spritesheet alongside its JSON file and places them into an Image and SplitSpriteInfo respectively.
    /// </summary>
    public static class SplitSpriteFileReader
    {
        private const string SPRITE_SHEET_PATH_SUFFIX = "_sheet.png";
        private const string SPRITE_JSON_PATH_SUFFIX = "_info.json";

        private const string JSON_SHEET_POS = "sheet_position";
        private const string JSON_SIZE = "size";
        private const string JSON_SPLIT_INDEX = "split_index";
        private const string JSON_SPLIT_POS = "split_position";
        private const string JSON_Z_INDEX = "z_index";
        private const string JSON_ANIM_NAME = "anim_name";
        private const string JSON_ANIM_FRAME = "anim_frame";

        /// <summary>
        /// Given a path to a spritesheet PNG attempts to open it as an image.
        /// </summary>
        /// <param name="spritePathPrefix"></param>
        /// <returns>An Image containing the PNG pointed to by the input <paramref name="spritePathPrefix"/>.</returns>
        public static Texture OpenSplitSpriteImage(this string spritePathPrefix)
        {
            return ResourceLoader.Load(spritePathPrefix + SPRITE_SHEET_PATH_SUFFIX) as Texture;
        }

        /// <summary>
        /// Given a path to a spritesheet's accompanying JSON, attempts to open and parse it.
        /// </summary>
        /// <param name="spritePathPrefix"></param>
        /// <returns>A list of SplitSpriteInfo representing the data in the JSON pointed to by the input <paramref name="spritePathPrefix"/>.</returns>
        public static List<SplitSpriteInfo> OpenSplitSpriteJson(this string spritePathPrefix)
        { 
            var jsonStreamReader = new StreamReader(ProjectSettings.GlobalizePath(spritePathPrefix + SPRITE_JSON_PATH_SUFFIX));
            string fullJSON = jsonStreamReader.ReadToEnd();
            var a = JArray.Parse(fullJSON);

            var allInfo = a.Select(token => new SplitSpriteInfo
            {
                sheetPosition = ((string)token[JSON_SHEET_POS]).StrToVec2(),
                size = ((string)token[JSON_SIZE]).StrToVec2(),
                splitIndex = (int)token[JSON_SPLIT_INDEX],
                splitPosition = ((string)token[JSON_SPLIT_POS]).StrToVec2(),
                zIndex = (int)token[JSON_Z_INDEX],
                animName = (string)token[JSON_ANIM_NAME],
                animFrame = (int)token[JSON_ANIM_FRAME]
            }).ToList();
            foreach (SplitSpriteInfo info in allInfo)
            {
                GD.PrintS("sheetPos: " + info.sheetPosition + ", size: " + info.size + ", splitIndex: " + info.splitIndex + ", splitPos: " + info.splitPosition + ", zIndex: " + info.zIndex + ", animName: " + info.animName + ", animFrame: " + info.animFrame);
            }
            jsonStreamReader.Close();
            return allInfo;
        }
    }
}