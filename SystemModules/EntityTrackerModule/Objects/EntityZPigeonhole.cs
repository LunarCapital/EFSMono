using System.Collections.Generic;
using Godot;

namespace EFSMono.SystemModules.EntityTrackerModule.Objects
{
    /// <summary>
    /// A data structure that holds information for a specific entity.  This info takes the form of a list of HashSets, one for EACH possible Z index 
    /// within the currently loaded world.  If the specific entity enters a shape owned by an Area2D, the shape's integer RID is added to the HashSet
    /// with the matching Z index of said Area2D. Likewise, if the entity leaves a shape its integer RID is removed.
    /// 
    /// This is used to track whether an entity needs to fall or not, because if a shape's integer RID is removed from its matching HashSet and now
    /// said HashSet is empty, then this means that the entity is no longer on the floor of whatever Z Index the shape was a part of.  If said Z 
    /// Index matches the entity's Z index, then it needs to fall.
    /// </summary>
    class EntityZPigeonhole
    {
        public int entityID { get; }
        private SortedList<int, HashSet<int>> _zIndexToAreaShapeID; //note that this is the shape's RID ID, not its index within the area2d.

        public EntityZPigeonhole(int entityID, int numTileMaps)
        {
            this.entityID = entityID;
            this._zIndexToAreaShapeID = new SortedList<int, HashSet<int>>();

            for (int i = 0; i < numTileMaps; i++)
            {
                this._zIndexToAreaShapeID[i] = new HashSet<int>();
            }
        }

        public void EnteredAreaShape(int zIndex, int shapeIndex)
        {
            if (this._zIndexToAreaShapeID.ContainsKey(zIndex))
            {
                this._zIndexToAreaShapeID[zIndex].Add(shapeIndex);
            }
        }

        public void ExitedAreaShape(int zIndex, int shapeIndex)
        {
            if (this._zIndexToAreaShapeID.ContainsKey(zIndex))
            {
                this._zIndexToAreaShapeID[zIndex].Remove(shapeIndex);
            }
        }

        public bool IsZEmpty(int zIndex)
        {
            return this._zIndexToAreaShapeID[zIndex].Count == 0;
        }
    }
}
