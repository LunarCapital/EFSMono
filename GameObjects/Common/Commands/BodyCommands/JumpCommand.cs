using EFSMono.Common.Autoload;
using EFSMono.Entities.Common.Components.Gravity;
using Godot;

namespace EFSMono.Entities.Common.Commands.BodyCommands
{
    public class JumpCommand : BaseCommand
    {
        private readonly GravityComponent _gravityComponent;
        private readonly int _initialJumpVelocity;

        public JumpCommand(GravityComponent gravityComponent, int initialJumpVelocity)
        {
            this._gravityComponent = gravityComponent;
            this._initialJumpVelocity = initialJumpVelocity;
        }

        public override void Execute()
        {
            this._gravityComponent.fallVelocity = this._initialJumpVelocity;
            this._gravityComponent.inAir = true;
        }
    }
}
