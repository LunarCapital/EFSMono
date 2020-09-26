using EFSMono.Entities.Common.Components.Controller;
using EFSMono.GameObjects.Common.Components.Stats;
using EFSMono.Preloading.Stats;
using Godot;

namespace EFSMono.GameObjects.Common.Components.Action
{
    public class MoveAction : ActionComponent
    {
        public override void Execute(Entity entity)
        {
            StatsComponent stats = entity.statsComponent;
            IController controller = entity.controller;

            float speed = stats.GetTotalStat((int)StatsEnumerator.StatID.MoveSpeed);
            Vector2 motion = controller.motion;
            entity.MoveAndSlide(motion * speed);
        }
    }
}
