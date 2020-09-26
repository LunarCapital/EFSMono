using EFSMono.Entities.Common.Commands.BodyCommands;
using EFSMono.GameObjects;
using Godot;

namespace EFSMono.Entities.Common.Components.Gravity
{
    public class PlayerGravityComponent : GravityComponent
    {
        public new const int Gravity = 16;
        public new const int BonusGrav = 320;
        public new const int InitialJumpVelocity = -12;

        public override bool inAir { get; protected internal set; }
        public override bool raiseHeight { get; protected set; }

        public override float airTime { get; protected set; }
        public override float fallVelocity { get; protected internal set; }
        public override float spriteDrawHeightPos { get; protected set; }

        public PlayerGravityComponent(Entity parent) : base(parent) {}

        public override void ProcessFall(float delta)
        {
            base.ProcessFall(delta);
            if (!this.inAir && Input.IsActionPressed("move_jump"))
            {
                var jumpCommand = new JumpCommand(this, InitialJumpVelocity);
                jumpCommand.Execute();
            }
        }
    }
}
