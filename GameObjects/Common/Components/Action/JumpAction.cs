using EFSMono.Entities.Common.Components.Gravity;
using StatID = EFSMono.Preloading.Stats.StatsEnumerator.StatID;

namespace EFSMono.GameObjects.Common.Components.Action
{
    public class JumpAction : ActionComponent
    {
        public override void Execute(Entity entity)
        {
            GravityComponent gravityComponent = entity.gravityComponent;

            gravityComponent.fallVelocity = entity.statsComponent.GetTotalStat(StatID.InitJumpVel);
            gravityComponent.inAir = true;
        }
    }
}
