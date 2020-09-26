using EFSMono.Entities.Common.Components.Controller;
using EFSMono.GameObjects.Common.Components.Stats;
using Godot;
using StatID = EFSMono.Preloading.Stats.StatsEnumerator.StatID;

namespace EFSMono.GameObjects.Common.Components.Action
{
    /// <summary>
    /// Action component for movement.
    /// </summary>
    public class MoveAction : ActionComponent
    {
        public override void Execute(Entity entity)
        {
            StatsComponent stats = entity.statsComponent;
            IController controller = entity.controller;

            float speed = stats.GetTotalStat(StatID.MoveSpeed);
            Vector2 motion = controller.motion;
            entity.MoveAndSlide(motion * speed);
        }
    }
}
