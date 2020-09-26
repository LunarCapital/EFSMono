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
            if (Input.IsActionPressed("move_dash") && motion != Vector2.Zero)
            {
                return new DashPlayerState(this._parent, motion, 0.15F); //TODO put maxdashtime in a params component, holy fuck i'm coupling like crazy this could be bad
            }
            else if (motion != Vector2.Zero)
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
