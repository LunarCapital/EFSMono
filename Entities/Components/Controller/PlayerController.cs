using System;
using EFSMono.Entities.Commands.BodyCommands;
using Godot;

namespace EFSMono.Entities.Components.Controller
{
    /// <summary>
    /// A player controller component that maps external player input to their respective command objects.
    /// </summary>
    public class PlayerController : IController
    {
        private readonly Entity _parent;
        public PlayerController(Entity parent)
        {
            this._parent = parent;
        }

        public void ParseInput()
        {
            var moveCommand = new MoveCommand(this._parent, _GetMoveInput(), 150); //get rid of hardcoded speed one day. maybe a stats or parameters component?
            moveCommand.Execute();
        }

        private static Vector2 _GetMoveInput()
        {
            var motion = new Vector2(0, 0);
            if (Input.IsActionPressed("move_up"))
                motion += new Vector2(0, -1);
            if (Input.IsActionPressed("move_down"))
                motion += new Vector2(0, 1);
            if (Input.IsActionPressed("move_left"))
                motion += new Vector2(-1, 0);
            if (Input.IsActionPressed("move_right"))
                motion += new Vector2(1, 0);
            if (motion.x != 0 && motion.y != 0)
            {
                motion.x = motion.x / Math.Abs(motion.x) * 2;
                motion.y = motion.y / Math.Abs(motion.y) * 1;
            }
            return motion.Normalized();
        }

    }
}
