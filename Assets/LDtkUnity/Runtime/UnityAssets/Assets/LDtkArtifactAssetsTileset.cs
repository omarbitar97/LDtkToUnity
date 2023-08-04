﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LDtkUnity
{
    public sealed class LDtkArtifactAssetsTileset : ScriptableObject
    {
        internal const string PROPERTY_SPRITE_LIST = nameof(_sprites);
        internal const string PROPERTY_TILE_LIST = nameof(_tiles);
        internal const string PROPERTY_ADDITIONAL_SPRITES = nameof(_additionalSprites);
        
        [SerializeField] internal List<Sprite> _sprites;
        [SerializeField] internal List<LDtkTilesetTile> _tiles;
        [SerializeField] internal List<Sprite> _additionalSprites;
        
        // This class doesn't contain malformed shapes.
        // There isn't an easy way to index them in an optimized way when it comes to serialization 

        /// <summary>
        /// Indexed by tile id
        /// </summary>
        public IReadOnlyList<Sprite> Sprites => _sprites;

        /// <summary>
        /// Indexed by tile id
        /// </summary>
        public IReadOnlyList<LDtkTilesetTile> Tiles => _tiles;
        
        /// <summary>
        /// These sprites are slices created from tile instances or any other situations that result in a larger area selection.
        /// Not indexed; Perform a lookup by comparing this list's rectangles.
        /// </summary>
        public IReadOnlyList<Sprite> AdditionalSprites => _additionalSprites;

        internal Dictionary<Rect, Sprite> AdditionalSpritesToDict()
        {
            return _additionalSprites.ToDictionary(sprite => sprite.rect);
        }
        
        internal Sprite GetAdditionalSpriteForRectSlow(Rect rect, int pxHei)
        {
            return _additionalSprites.FirstOrDefault(p => p.rect == LDtkCoordConverter.ImageSlice(rect, pxHei));
        }
    }
}