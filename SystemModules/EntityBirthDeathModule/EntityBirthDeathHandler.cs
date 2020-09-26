using System.Linq;
using EFSMono.SystemModules.EntityBirthDeathModule.HubMessages;
using EFSMono.SystemModules.TileProcessorModule.TileProcessorObjects;
using Godot;
using TinyMessenger;
using System.Collections.Generic;
using EFSMono.GameObjects;

namespace EFSMono.SystemModules.EntityBirthDeathModule
{
    /// <summary>
    /// A class that maintains a dictionary of Entities, adding and removing to said dictionary when entities are created and killed.
    /// Also can return an IMMUTABLE object that contains a reference to this class's dictionary of Entities, allowing other modules 
    /// readonly access to living entities.
    /// </summary>
    class EntityBirthDeathHandler
    {
        private Dictionary<int, Entity> _entitiesByID;
        private TinyMessengerHub _entityBirthDeathWatcherHub;
        private TinyMessengerHub _entityBirthDeathForwarderHub;

        private TinyMessageSubscriptionToken _onEntityBornToken;
        private TinyMessageSubscriptionToken _onEntityDiedToken;

        /// <summary>
        /// Called when a new world is loaded.
        /// Takes <paramref name="tileMaps"/> as an input and searches for all the entities within said input to store them in this class's 
        /// dictionary of Entities.
        /// </summary>
        /// <param name="tileMaps"></param>
        public void LoadWorld(TileMapList tileMaps)
        {
            this._entitiesByID = new Dictionary<int, Entity>();
            foreach (TileMap tileMap in tileMaps.Values)
            {
                foreach (Entity entity in tileMap.GetChildren().OfType<Entity>()) 
                {
                    this._entitiesByID.Add(entity.GetRid().GetId(), entity);
                }
            }
            this._entityBirthDeathWatcherHub = new TinyMessengerHub();
            this._onEntityBornToken = this._entityBirthDeathWatcherHub.Subscribe<EntityBornMessage>(this._OnEntityBorn);
            this._onEntityDiedToken = this._entityBirthDeathWatcherHub.Subscribe<EntityDeathMessage>(this._OnEntityDied);

            this._entityBirthDeathForwarderHub = new TinyMessengerHub();
        }

        public void UnloadWorld()
        {
            if (!(this._onEntityBornToken is null))
            {
                this._entityBirthDeathWatcherHub.Unsubscribe(this._onEntityBornToken);
                this._onEntityBornToken.Dispose();
            }
            if (!(this._onEntityDiedToken is null))
            {
                this._entityBirthDeathWatcherHub.Unsubscribe(this._onEntityDiedToken);
                this._onEntityDiedToken.Dispose();
            }
        }

        public ReadonlyEntityPackage GetEntityPackage()
        {
            var entityPackage = new ReadonlyEntityPackage(this._entitiesByID, this._entityBirthDeathForwarderHub);
            return entityPackage;
        }

        //event method, add from entities
        private void _OnEntityBorn(EntityBornMessage msg)
        {
            this._entitiesByID.Add(msg.entity.GetRid().GetId(), msg.entity);
            this._entityBirthDeathForwarderHub.Publish(msg);
        }

        //event method, delete from entities
        private void _OnEntityDied(EntityDeathMessage msg)
        {
            if (this._entitiesByID.ContainsKey(msg.entity.GetRid().GetId()))
            {
                this._entitiesByID.Remove(msg.entity.GetRid().GetId());
                this._entityBirthDeathForwarderHub.Publish(msg);
            }
        }

    }
}
