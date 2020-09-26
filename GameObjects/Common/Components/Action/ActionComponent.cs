using System;
using System.Collections.Generic;
using EFSMono.Entities.Common.Components.Controller;
using EFSMono.GameObjects.Common.Components.Stats;

namespace EFSMono.GameObjects.Common.Components.Action
{
    /// <summary>
    /// A class that contains stats and a command.
    /// SHOULD be loaded from a config/json file.
    /// </summary>
    public abstract class ActionComponent
    {
        private readonly Dictionary<int, float> _baseStats;
        private readonly Dictionary<int, float> _bonusStats;

        public ActionComponent()
        {
            this._baseStats = new Dictionary<int, float>();
            this._bonusStats = new Dictionary<int, float>();
        }

        public abstract void Execute(Entity entity);
    }
}