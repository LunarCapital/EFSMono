using System.Collections.Generic;
using EFSMono.Common.IO;
using Godot;
using Newtonsoft.Json.Linq;

namespace EFSMono.GameObjects.Common.IO
{
    /// <summary>
    /// A class that exists to read in the json config file that defines an entity's stats.
    /// </summary>
    public static class StatReader
    {
        private const string JsonID = "id";
        private const string JsonValue = "value";

        /// <summary>
        /// Reads in the json config file with the path <paramref name="filePath"/> and returns a dictionary mapping its IDs to values.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Dictionary<int, float> ReadStats(string filePath)
        {
            JArray jArray = JArrayReader.ReadJArray(filePath);

            foreach (JToken jToken in jArray.Children())
            {
                GD.PrintS("jtoken reads: " + jToken.ToString());
            }

            return new Dictionary<int, float>();
        }

    }
}
