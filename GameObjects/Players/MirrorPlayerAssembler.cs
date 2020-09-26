using EFSMono.Entities.Common.Components.Controller;
using EFSMono.Entities.Common.Components.Gravity;
using EFSMono.GameObjects.Common.Components.Controller;
using EFSMono.GameObjects.Common.Components.Stats;
using EFSMono.GameObjects.Common.IO;
using Godot;

namespace EFSMono.GameObjects.Players
{
    /// <summary>
    /// Component assembler for the mirror player.
    /// </summary>
    public class MirrorPlayerAssembler : EntityAssembler
    {
        private const string StatsFilePath = "./GameObjects/Players/stats.json";

        public override StatsComponent CreateStatsComp()
        {
            GD.PrintS("i am called. name is: ");
            StatReader.ReadStats(StatsFilePath);
            return new StatsComponent();
        }

        public override IController CreateController(Entity parent)
        {
            return new MirrorPlayerController(parent);
        }

        public override GravityComponent CreateGravityComp(Entity parent)
        {
            return new PlayerGravityComponent(parent);
        }

    }
}
