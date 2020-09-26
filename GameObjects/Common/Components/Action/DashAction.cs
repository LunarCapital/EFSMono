using Godot;
using StatID = EFSMono.Preloading.Stats.StatsEnumerator.StatID;

namespace EFSMono.GameObjects.Common.Components.Action
{
    /// <summary>
    /// Action component for dash.
    /// </summary>
    public class DashAction : ActionComponent
    {
        private Vector2 _direction;
        private float _elapsedTime;

        public DashAction()
        {
            this._elapsedTime = 0;
        }

        public void SetDirection(Vector2 direction)
        {
            this._direction = direction;
        }

        public void UpdateElapsedTime(float elapsedTime)
        {
            this._elapsedTime = elapsedTime;
        }

        public override void Execute(Entity entity)
        {
            float maxSpeed = entity.statsComponent.GetTotalStat(StatID.DashSpeed);
            float maxTime = entity.statsComponent.GetTotalStat(StatID.DashTime);
            float speed = maxSpeed - this._elapsedTime * maxSpeed / maxTime;

            entity.MoveAndSlide(this._direction * speed);
        }
    }
}
