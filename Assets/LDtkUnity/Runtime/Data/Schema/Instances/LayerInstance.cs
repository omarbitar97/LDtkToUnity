﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace LDtkUnity.Data
{
    public class LayerInstance : ILDtkIdentifier
    {
        /// <summary>
        /// Grid-based height
        /// </summary>
        [JsonProperty("__cHei")]
        public long CHei { get; set; }

        /// <summary>
        /// Grid-based width
        /// </summary>
        [JsonProperty("__cWid")]
        public long CWid { get; set; }

        /// <summary>
        /// Grid size
        /// </summary>
        [JsonProperty("__gridSize")]
        public long GridSize { get; set; }

        /// <summary>
        /// Unique String identifier
        /// </summary>
        [JsonProperty("__identifier")]
        public string Identifier { get; set; }

        /// <summary>
        /// Layer opacity as Float [0-1]
        /// </summary>
        [JsonProperty("__opacity")]
        public double Opacity { get; set; }

        /// <summary>
        /// Total layer X pixel offset, including both instance and definition offsets.
        /// </summary>
        [JsonProperty("__pxTotalOffsetX")]
        public long PxTotalOffsetX { get; set; }

        /// <summary>
        /// Total layer Y pixel offset, including both instance and definition offsets.
        /// </summary>
        [JsonProperty("__pxTotalOffsetY")]
        public long PxTotalOffsetY { get; set; }

        /// <summary>
        /// The definition UID of corresponding Tileset, if any.
        /// </summary>
        [JsonProperty("__tilesetDefUid")]
        public long? TilesetDefUid { get; set; }

        /// <summary>
        /// The relative path to corresponding Tileset, if any.
        /// </summary>
        [JsonProperty("__tilesetRelPath")]
        public string TilesetRelPath { get; set; }

        /// <summary>
        /// Layer type (possible values: IntGrid, Entities, Tiles or AutoLayer)
        /// </summary>
        [JsonProperty("__type")]
        public string Type { get; set; }

        /// <summary>
        /// An array containing all tiles generated by Auto-layer rules. The array is already sorted
        /// in display order (ie. 1st tile is beneath 2nd, which is beneath 3rd etc.).<br/><br/>
        /// Note: if multiple tiles are stacked in the same cell as the result of different rules,
        /// all tiles behind opaque ones will be discarded.
        /// </summary>
        [JsonProperty("autoLayerTiles")]
        public TileInstance[] AutoLayerTiles { get; set; }

        [JsonProperty("entityInstances")]
        public EntityInstance[] EntityInstances { get; set; }

        [JsonProperty("gridTiles")]
        public TileInstance[] GridTiles { get; set; }

        [JsonProperty("intGrid")]
        public Dictionary<string, dynamic>[] IntGrid { get; set; }

        /// <summary>
        /// Reference the Layer definition UID
        /// </summary>
        [JsonProperty("layerDefUid")]
        public long LayerDefUid { get; set; }

        /// <summary>
        /// Reference to the UID of the level containing this layer instance
        /// </summary>
        [JsonProperty("levelId")]
        public long LevelId { get; set; }

        /// <summary>
        /// X offset in pixels to render this layer, usually 0 (IMPORTANT: this should be added to
        /// the `LayerDef` optional offset, see `__pxTotalOffsetX`)
        /// </summary>
        [JsonProperty("pxOffsetX")]
        public long PxOffsetX { get; set; }

        /// <summary>
        /// Y offset in pixels to render this layer, usually 0 (IMPORTANT: this should be added to
        /// the `LayerDef` optional offset, see `__pxTotalOffsetY`)
        /// </summary>
        [JsonProperty("pxOffsetY")]
        public long PxOffsetY { get; set; }

        /// <summary>
        /// Random seed used for Auto-Layers rendering
        /// </summary>
        [JsonProperty("seed")]
        public long Seed { get; set; }
    }
}