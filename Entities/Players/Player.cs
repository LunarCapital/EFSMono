using System;
using EFSMono.Entities.Commands.BodyCommands;
using EFSMono.Entities.Components.Controller;
using Godot;

namespace EFSMono.Entities.Players
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
        public override IController controller { get; protected set; }

        public override void _Ready()
        {
            this.controller = new PlayerController(this);
        }

        public override void _PhysicsProcess(float delta)
        {
            base._PhysicsProcess(delta);
            if (!this.falling && Input.IsActionPressed("move_jump"))
            {
                this.fallVelocity = -12;
                this.falling = true;
            }
            //this.controller.

        }
    }
}