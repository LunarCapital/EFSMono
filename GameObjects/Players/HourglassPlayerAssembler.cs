using EFSMono.Entities.Common.Components.Controller;
using EFSMono.Entities.Common.Components.Gravity;
using EFSMono.GameObjects.Common.Components.Stats;

namespace EFSMono.GameObjects.Players
{
    public class HourglassPlayerAssembler : EntityAssembler
    {
        private const string StatsFilePath = "./GameObjects/Players/stats.json";

        public override StatsComponent CreateStatsComp()
        {
            //TODO controller doesn't read in anything yet
            return base.CreateStatsComp();
        }

        public override IController CreateController(Entity parent)
        {
            return base.CreateController(parent);
        }

        public override GravityComponent CreateGravityComp(Entity parent)
        {
            return new PlayerGravityComponent(parent);
        }
    }
}
