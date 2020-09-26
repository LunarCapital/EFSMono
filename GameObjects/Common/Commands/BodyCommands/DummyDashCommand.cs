using EFSMono.GameObjects;
using EFSMono.GameObjects.Common.Components.Action;
using EFSMono.GameObjects.Common.Components.Stats;
using Godot;

namespace EFSMono.Entities.Common.Commands.BodyCommands
{
    /// <summary>
    /// Command that executes a dash for some subject entity.
    /// </summary>
    public class DummyDashCommand : BaseCommand
    {
        private readonly Entity _subject;
        private readonly Vector2 _motion;
        private readonly int _speed;

        public DummyDashCommand(StatsComponent stats, ActionComponent action, Entity entity) : base(stats, action, entity)
        {

        }

        public DummyDashCommand(Entity subject, Vector2 motion, int speed) : base()
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
