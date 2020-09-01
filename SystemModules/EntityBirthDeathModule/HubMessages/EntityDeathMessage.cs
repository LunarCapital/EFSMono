using EFSMono.Entities;
using TinyMessenger;

namespace EFSMono.SystemModules.EntityBirthDeathModule.HubMessages
{
    public class EntityDeathMessage : ITinyMessage
    {
        public object Sender { get; }
        public Entity entity { get; }

        public EntityDeathMessage(Entity entity)
        {
            this.entity = entity;
        }
    }
}