using System.Collections.Generic;
using EFSMono.Entities.Common.Components.Controller;
using EFSMono.Entities.Players.InputParser;
using EFSMono.Entities.Players.States;
using EFSMono.GameObjects.Common.Components.Action;
using Godot;
using ActionID = EFSMono.Common.Enums.Actions.ActionID;

namespace EFSMono.GameObjects.Common.Components.Controller
{
    /// <summary>
    /// A player controller component that handles the control scheme for the player by maintaining its state, and delegating to said
    /// state the responsibilty of mapping player input to command objects.
    /// </summary>
    public class MirrorPlayerController : IController
    {
        public Vector2 motion { get; private set; }
        public Vector2 target { get; private set; }
        private readonly Dictionary<ActionID, ActionComponent> _actions;

        private readonly Entity _parent;
        private IPlayerState _state;

        public MirrorPlayerController(Entity parent)
        {
            this._actions = new Dictionary<ActionID, ActionComponent>();
            //TODO REPLACE WITH JSON LOADING
            this._actions[ActionID.Move] = new MoveAction();
            this._actions[ActionID.Dash] = new DashAction();
            this._actions[ActionID.Jump] = new JumpAction();
            //END REPLACE WITH JSON LOADING
            this._parent = parent;
            this._state = new IdlePlayerState(this._parent);
        }

        public void ControlEntity(float delta)
        {
            this.motion = MotionParser.GetMoveInput();
            this.target = Vector2.Zero;
            this._state = this._state.ParseInput(delta, this.motion, this.target);
        }

        public ActionComponent GetAction(ActionID actionID)
        {
            return this._actions[actionID];
        }
    }
}
