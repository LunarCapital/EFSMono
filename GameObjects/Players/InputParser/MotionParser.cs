using System;
using Godot;

namespace EFSMono.Entities.Players.InputParser
{
    public static class MotionParser
    {
        public static Vector2 GetMoveInput()
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
