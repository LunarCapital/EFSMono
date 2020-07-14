using Godot;
using System;

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
        var motion = new Vector2(0, 0);
        if (Input.IsActionPressed("move_up")) { // change these to global constants?
            motion += new Vector2(0, -1);
        }
        if (Input.IsActionPressed("move_down")) {
            motion += new Vector2(0, 1);
        }
        if (Input.IsActionPressed("move_left")) {
            motion += new Vector2(-2, 0);
        }
        if (Input.IsActionPressed("move_right")) {
            motion += new Vector2(2, 0);
        }
        motion = motion.Ceil();
        this.MoveAndSlide(motion*10);
        //TODO: ensure entity positions are rounded to prevent jitter
    }
}
