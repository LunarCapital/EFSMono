using System;
using System.Collections.Generic;
using Godot;

namespace EFSMono.Common.Autoload
{
    /// <summary>
    /// Autoload script.
    /// Contains easily accessible constants and methods related to Collision Layers & Masks.
    /// </summary>
    public class LayersFuncs : Node
    {
        //COLLISION LAYERS
        [Flags]
        public enum CollisionLayers
        {
            Terrain = 0,
            PlayerEntity = 1,
            NpcEntity = 2
        };

        /// <summary>
        /// Gets value of multiple input layers by recursively adding 2^(layer value) and returning the result.
        /// </summary>
        /// <param name="allLayers">As many CollisionLayers as needed.</param>
        /// <returns>uint describing value of all input layers.</returns>
        public static uint GetLayersValue(params CollisionLayers[] allLayers)
        {
            uint value = 0;
            var usedLayers = new HashSet<CollisionLayers>();
            foreach (CollisionLayers layer in allLayers)
            {
                if (usedLayers.Contains(layer)) continue;
                value += (uint)Math.Pow(2, (int)layer);
                usedLayers.Add(layer);
            }
            return value;
        }
    }
}