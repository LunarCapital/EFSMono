using System.Globalization;
using Godot;

namespace EFSMono.Common.Extensions
{
    public static class Vector2Extensions
    {
        /// <summary>
        /// Given an input string, converts it to a Vector2.
        /// Does not check for validity because surely I will never misuse this ever
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Vector2 StrToVec2(this string str)
        {
            int leftBracketIndex = str.Find("(");
            int commaIndex = str.Find(",");
            int rightBracketIndex = str.Find(")");

            float x = float.Parse(str.Substr(leftBracketIndex + 1, commaIndex - leftBracketIndex - 1), NumberFormatInfo.InvariantInfo);
            float y = float.Parse(str.Substr(commaIndex + 1, rightBracketIndex - commaIndex - 1), NumberFormatInfo.InvariantInfo);

            return new Vector2(x, y);
        }
    }
}