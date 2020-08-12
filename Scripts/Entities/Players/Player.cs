using System;
using Godot;

namespace EFSMono.Scripts.Entities.Players
{
/// <summary>
/// Generic player class.
/// Considering breaking up into smaller scripts:
///     Input handling
///     Movement
///     Actions
/// Or a flat out state machine? Won't implement much as of yet until decided
/// </summary>
public class Player : Entity
{
    public override void _Ready()
    {
    }

    public override void _PhysicsProcess(float delta)
    {
        Vector2 oldPos = this.Position;
        Vector2 motion = this._GetInput();
        this.MoveAndSlide(motion * 50);
        
        /*if (motion.x != 0 && motion.y != 0)
        {
            if (2*Math.Abs(oldPos.x - this.Position.x) > Math.Abs(oldPos.y - this.Position.y))
            {
                float roundX = Mathf.Round(this.Position.x);
                float roundY = Mathf.Round(this.Position.y + (roundX - this.Position.x) * motion.y / motion.x);
                this.Position = new Vector2(this.Position.x, roundY);
            }
            else if (2*Math.Abs(oldPos.x - this.Position.x) <= Math.Abs(oldPos.y - this.Position.y))
            {
                float roundY = Mathf.Round(this.Position.y);
                float roundX = Mathf.Round(this.Position.x + (roundY - this.Position.y) * motion.x / motion.y);
                this.Position = new Vector2(roundX, this.Position.y);
            }
        }*/
        //TODO: ensure entity positions are rounded to prevent jitter
    }

    private Vector2 _GetInput()
    {
        var motion = new Vector2(0, 0);
        if (Input.IsActionPressed("move_up")) { // change these to global constants?
            motion += new Vector2(0, -1);
        }
        if (Input.IsActionPressed("move_down")) {
            motion += new Vector2(0, 1);
        }
        if (Input.IsActionPressed("move_left")) {
            motion += new Vector2(-1, 0);
        }
        if (Input.IsActionPressed("move_right")) {
            motion += new Vector2(1, 0);
        }
        if (motion.x != 0 && motion.y != 0)
        {
            motion.x = motion.x / Math.Abs(motion.x) * 2;
            motion.y = motion.y / Math.Abs(motion.y) * 1;
        }
        return motion.Normalized();
    }
}
}
