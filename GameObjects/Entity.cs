using EFSMono.Entities.Common.Components.Controller;
using EFSMono.Entities.Common.Components.Gravity;
using EFSMono.GameObjects.Common.Components.Stats;
using Godot;

namespace EFSMono.GameObjects
{
    public sealed class Entity : KinematicBody2D
    {
        //COMPONENTS
        public StatsComponent statsComponent { get; private set; }
        public IController controller { get; private set; }
        public GravityComponent gravityComponent { get; private set; }
        //public List<ActionComponent> actions { get; private set; }

        public Sprite fullSprite { get; private set; }
        private EntityAssembler _assembler;

        public override void _Ready()
        {
            foreach (Node child in this.GetChildren())
            {
                if (child is Sprite sprite)
                {
                    this.fullSprite = sprite;
                }
                if (child is EntityAssembler assembler)
                {
                    this._assembler = assembler;
                }
            }

            this.statsComponent = this._assembler.CreateStatsComp();
            this.controller = this._assembler.CreateController(this);
            this.gravityComponent = this._assembler.CreateGravityComp(this);
        }

        // Called every frame. 'delta' is the elapsed time since the previous frame.
        public override void _PhysicsProcess(float delta)
        {
            this.controller.ControlEntity(delta);
            this.gravityComponent.ProcessFall(delta);
        }
    }
}