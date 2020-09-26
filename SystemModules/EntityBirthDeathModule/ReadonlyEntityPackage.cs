using System.Collections.Generic;
using EFSMono.GameObjects;
using Godot;
using TinyMessenger;

namespace EFSMono.SystemModules.EntityBirthDeathModule
{
    /// <summary>
    /// A class that stores a readonly reference to EntityBirthDeathHandler's dictionary of all entities.
    /// Also comes packaged with an event hub that is used by EntityBirthDeatHandler to notify any interested parties about entities being born or dying.
    /// 
    /// I actually wanted this to be immutable but lack the brainpower to figure out how to get each entity's Z index every frame, or to get some local entity ID -> zIndex
    /// to update fast enough so I'm not doing some O(2N) operation every physics process (or every frame), so currently I just return entitiesByID and do an O(N) operation
    /// every physics process instead, which still makes me nervous (but surely it's okay, people get 3D stuff to do draw updates every 16ms nowadays, right?).
    /// </summary>
    public class ReadonlyEntityPackage
    {
        public Dictionary<int, Entity> entitiesByID { get; }
        public TinyMessengerHub entitiesChangedHub { get; }

        public ReadonlyEntityPackage(Dictionary<int, Entity> entitiesByID, TinyMessengerHub entitiesChangedHub)
        {
            this.entitiesByID = entitiesByID;
            this.entitiesChangedHub = entitiesChangedHub;
        }

        public List<RID> GetAllRIDs()
        {
            var entityRIDs = new List<RID>();
            foreach (Entity entity in this.entitiesByID.Values)
            {
                entityRIDs.Add(entity.GetRid());
            }
            return entityRIDs;
        }

        public List<int> GetAllIDs()
        {
            var entityIDs = new List<int>();
            foreach (int id in this.entitiesByID.Keys)
            {
                entityIDs.Add(id);
            }
            return entityIDs;
        }

        /// <summary>
        /// Attempts to check the Z index of an entity given its ID.
        /// Returns -1 if said entity ID does not exist.
        /// </summary>
        /// <param name="entityID"></param>
        /// <returns></returns>
        public int GetZIndex(int entityID)
        {
            if (this.entitiesByID.ContainsKey(entityID))
            {
                Entity entity = this.entitiesByID[entityID];
                return ((TileMap)entity.GetParent()).ZIndex; //technically this is dangerous, might come back to it
            }
            else return - 1;
        }
    }
}
