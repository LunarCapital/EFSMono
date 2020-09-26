using EFSMono.Entities.Common.Commands.BodyCommands;
using EFSMono.Entities.Common.Components.Controller;
using EFSMono.GameObjects;
using Godot;

namespace EFSMono.Entities.Players.States
{
    class DashPlayerState : IPlayerState
    {
        private readonly Entity _parent;
        private readonly Vector2 _direction;
        private readonly float _dashMaxTime;
        private float _elapsedTime;

        public DashPlayerState(Entity parent, Vector2 direction, float dashMaxTime)
        {
            this._parent = parent;
            this._direction = direction;
            this._dashMaxTime = dashMaxTime;
            this._elapsedTime = 0;
        }

        public IPlayerState ParseInput(float delta, Vector2 motion, Vector2 target)
        {
            this._elapsedTime += delta;
            if (this._elapsedTime >= this._dashMaxTime)
            {
                return new IdlePlayerState(this._parent);
            }
            else
            {
                var dashCommand = new DashCommand(this._parent, this._direction, (int)(2000 - this._elapsedTime * 2000 / this._dashMaxTime)); //TODO put dashspeed in a params component or something
                GD.PrintS("speed: " + (2000 - this._elapsedTime * 2000 / this._dashMaxTime) + ", ET: " + this._elapsedTime);
                dashCommand.Execute();
                return this;
            }
        }
    }
}
