using EFSMono.GameObjects;
using Godot;
using ActionID = EFSMono.Common.Enums.Actions.ActionID;

namespace EFSMono.Entities.Players.States
{
    public class MovingPlayerState : IPlayerState
    {
        private readonly Entity _parent;

        public MovingPlayerState(Entity parent)
        {
            this._parent = parent;
        }

        public IPlayerState ParseInput(float delta, Vector2 motion, Vector2 target)
        {
            if (!this._parent.gravityComponent.inAir)
            { //MUST BE ON GROUND
                if (Input.IsActionPressed("move_dash") && motion != Vector2.Zero)
                {
                    return new DashPlayerState(this._parent, motion);
                }
                if (Input.IsActionPressed("move_jump"))
                {
                    this._parent.controller.GetAction(ActionID.Jump).Execute(this._parent);
                }

            }
            if (motion != Vector2.Zero)
            {
                this._parent.controller.GetAction(ActionID.Move).Execute(this._parent);
                return this;
            }
            else
            {
                return new IdlePlayerState(this._parent);
            }
        }
    }
}
