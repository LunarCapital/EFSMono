using EFSMono.Entities.Common.Commands;
using EFSMono.GameObjects;
using Godot;

namespace EFSMono.Entities.Common.Commands.BodyCommands
{
    /// <summary>
    /// Command that executes a move action.
    /// </summary>
    class MoveCommand : BaseCommand
    {
        private readonly Entity _subject;
        private readonly Vector2 _motion;
        private readonly int _speed;

        public MoveCommand(Entity subject, Vector2 motion, int speed)
        {
            this._subject = subject;
            this._motion = motion;
            this._speed = speed;
        }

        public override void Execute()
        {
            this._subject.MoveAndSlide(this._motion * this._speed);
        }
    }
}
