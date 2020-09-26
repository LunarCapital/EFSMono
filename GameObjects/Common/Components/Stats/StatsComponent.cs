using System;
using EFSMono.Preloading.Stats;
using Godot;
using Godot.Collections;
using StatID = EFSMono.Preloading.Stats.StatsEnumerator.StatID;

namespace EFSMono.GameObjects.Common.Components.Stats
{
    /// <summary>
    /// A component that acts as a container for an entity's stat values.
    /// </summary>
    public class StatsComponent
    {

        private readonly Dictionary<StatID, float> _stats;
        private readonly Dictionary<StatID, float> _bonus;

        /// <summary>
        /// Constructs stats using default values.
        /// </summary>
        public StatsComponent()
        {
            this._stats = new Dictionary<StatID, float>();
            this._bonus = new Dictionary<StatID, float>();
            foreach (StatID statID in Enum.GetValues(typeof(StatID)))
            {
                DefaultStat stat = StatsPreloader.GetStatByID((int)statID);
                this._stats.Add(statID, stat.defaultVal);
                this._bonus.Add(statID, 0);
            }

            GD.PrintS(Enum.GetNames(typeof(StatID)).Length);
        }

        /// <summary>
        /// Constructs stats using predefined values (originating from a config json file probably) passed in as a dictionary.
        /// </summary>
        /// <param name="readStats"></param>
        public StatsComponent(Dictionary<int, float> readStats) : this()
        {
            if (readStats is null) throw new ArgumentNullException(nameof(readStats));
            foreach (int id in readStats.Keys)
            {
                this._stats[(StatID)id] = readStats[id];
            }
        }

        /// <summary>
        /// Returns the sum of the base stat and the bonus value for said stat for some given input <paramref name="id"/>.
        /// </summary>
        /// <param name="id">ID of </param>
        /// <returns></returns>
        public float GetTotalStat(StatID id)
        {
            return this._stats[id] + this._bonus[id];
        }

    }
}
