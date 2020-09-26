using EFSMono.GameObjects;
using EFSMono.GameObjects.Common.Components.Action;
using Godot;
using ActionID = EFSMono.Common.Enums.Actions.ActionID;
using StatID = EFSMono.Preloading.Stats.StatsEnumerator.StatID;

namespace EFSMono.Entities.Players.States
{
    class DashPlayerState : IPlayerState
    {
        private readonly Entity _parent;
        private readonly Vector2 _direction;
        private float _elapsedTime;

        public DashPlayerState(Entity parent, Vector2 direction)
        {
            this._parent = parent;
            this._direction = direction;
            this._elapsedTime = 0;

            var dashAction = (DashAction)this._parent.controller.GetAction(ActionID.Dash);
            dashAction.SetDirection(this._direction);
        }

        public IPlayerState ParseInput(float delta, Vector2 motion, Vector2 target)
        {
            this._elapsedTime += delta;
            if (this._elapsedTime >= this._parent.statsComponent.GetTotalStat(StatID.DashTime))
            {
                return new IdlePlayerState(this._parent);
            }
            else
            {
                var dashAction = (DashAction)this._parent.controller.GetAction(ActionID.Dash);
                dashAction.UpdateElapsedTime(this._elapsedTime);
                dashAction.Execute(this._parent);
                return this;
            }
        }
    }
}
