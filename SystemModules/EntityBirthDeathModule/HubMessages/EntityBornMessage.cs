using EFSMono.GameObjects;
using TinyMessenger;

namespace EFSMono.SystemModules.EntityBirthDeathModule.HubMessages
{
    public class EntityBornMessage : ITinyMessage
    {
        public object Sender { get; }
        public Entity entity { get; }

        public EntityBornMessage(Entity entity)
        {
            this.entity = entity;
        }
    }
}