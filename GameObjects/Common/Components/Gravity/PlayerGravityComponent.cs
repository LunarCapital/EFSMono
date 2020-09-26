using EFSMono.GameObjects;

namespace EFSMono.Entities.Common.Components.Gravity
{
    public class PlayerGravityComponent : GravityComponent
    {
        public override bool inAir { get; protected internal set; }
        public override bool raiseHeight { get; protected set; }

        public override float airTime { get; protected set; }
        public override float fallVelocity { get; protected internal set; }
        public override float spriteDrawHeightPos { get; protected set; }

        public PlayerGravityComponent(Entity parent) : base(parent) {}

        public override void ProcessFall(float delta)
        {
            base.ProcessFall(delta);
        }
    }
}
