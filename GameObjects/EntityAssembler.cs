using EFSMono.Entities.Common.Components.Controller;
using EFSMono.Entities.Common.Components.Gravity;
using EFSMono.GameObjects.Common.Components.Controller;
using EFSMono.GameObjects.Common.Components.Stats;
using Godot;

namespace EFSMono.GameObjects
{
    /// <summary>
    /// A abstract class that 'assembles' the various components that make up an Entity.
    /// Named an 'assembler' and not a factory or builder because it does not make the Entity, it makes its components.
    /// </summary>
    public abstract class EntityAssembler : Node2D
    {
        public virtual StatsComponent CreateStatsComp()
        {
            return new StatsComponent();
        }

        public virtual IController CreateController(Entity parent)
        {
            return new PlayerController(parent);
        }

        public virtual GravityComponent CreateGravityComp(Entity parent)
        {
            return new PlayerGravityComponent(parent);
        }

        /*
        public void CreateActions()
        {

        }*/
    }
}
