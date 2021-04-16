﻿using System.Linq;

namespace LDtkUnity
{
    public partial class Definitions
    {
        /// <summary>
        /// All IntGrid layer definitions. (Empty if none are defined)
        /// </summary>
        public LayerDefinition[] EntityLayers => Layers.Where(p => p.IsEntitiesLayer).ToArray();
        
        /// <summary>
        /// All IntGrid layer definitions. (Empty if none are defined)
        /// </summary>
        public LayerDefinition[] IntGridLayers => Layers.Where(p => p.IsIntGridLayer).ToArray();
        
        /// <summary>
        /// All Auto-Layer definitions. (Empty if none are defined)
        /// </summary>
        public LayerDefinition[] AutoLayers => Layers.Where(p => p.IsAutoLayer).ToArray();

        /// <summary>
        /// All Tile layer definitions. (Empty if none are defined)
        /// </summary>
        public LayerDefinition[] TilesLayers => Layers.Where(p => p.IsTilesLayer).ToArray();

        /// <summary>
        /// All layers that would utilize a Grid prefab in the level creation process. (Empty if none are defined)
        /// </summary>
        public LayerDefinition[] UnityGridLayers => Layers.Where(p => p.IsIntGridLayer || p.IsAutoLayer || p.IsTilesLayer).ToArray();
    }
}