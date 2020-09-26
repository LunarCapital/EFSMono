using EFSMono.GameObjects;
using Godot;

namespace EFSMono.Entities.Common.Commands.BodyCommands
{
    /// <summary>
    /// Command that executes a dash for some subject entity.
    /// </summary>
    public class DashCommand : BaseCommand
    {
        private readonly Entity _subject;
        private readonly Vector2 _motion;
        private readonly int _speed;

        public DashCommand(Entity subject, Vector2 motion, int speed)
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
