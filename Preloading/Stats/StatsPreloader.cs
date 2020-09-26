using System.Collections.Generic;
using System.Linq;
using EFSMono.Common.IO;
using Godot;
using Newtonsoft.Json.Linq;

namespace EFSMono.Preloading.Stats
{
    /// <summary>
    /// A class that preloads stats and stores them into memory.
    /// Static because every subclass of EntityBuilder needs access to it.
    /// </summary>
    public static class StatsPreloader
    {
        public const string JsonID = "id";
        public const string JsonName = "name";
        public const string JsonDefault = "default";

        private static readonly SortedDictionary<int, DefaultStat> _stats = new SortedDictionary<int, DefaultStat>();

        public static void Preload(string statsPath)
        {

            JArray jArray = JArrayReader.ReadJArray(statsPath);

            var allInfo = jArray.Select(token => new DefaultStat(
                (int)token[JsonID],
                (string)token[JsonName],
                (float)token[JsonDefault]
            )).ToList();

            foreach (DefaultStat stat in allInfo)
            {
                _stats.Add(stat.id, stat);
                GD.PrintS("Stat has id: " + stat.id + ", name: " + stat.name + ", default: " + stat.defaultVal);
            }
        }

        /// <summary>
        /// Returns the stat with the input <paramref name="id"></paramref>.
        /// Note there is no error checking, if an invalid id is input then the dictionary's exception will be thrown.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static DefaultStat GetStatByID(int id)
        {
            return _stats[id];
        }
    }
}
