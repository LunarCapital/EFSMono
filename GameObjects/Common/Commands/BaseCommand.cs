using EFSMono.GameObjects;
using EFSMono.GameObjects.Common.Components.Action;
using EFSMono.GameObjects.Common.Components.Stats;

namespace EFSMono.Entities.Common.Commands
{
    /// <summary>
    /// Command base class.
    /// </summary>
    public abstract class BaseCommand
    {
        public abstract void Execute();
        public BaseCommand() { }
        public BaseCommand(StatsComponent stats, ActionComponent action, Entity entity) { }

    }
}