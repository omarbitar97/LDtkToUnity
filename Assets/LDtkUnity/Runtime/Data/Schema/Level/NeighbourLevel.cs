﻿using System.Runtime.Serialization;

namespace LDtkUnity
{
    /// <summary>
    /// Nearby level info
    /// </summary>
    public partial class NeighbourLevel
    {
        /// <summary>
        /// A single lowercase character tipping on the level location (`n`orth, `s`outh, `w`est,
        /// `e`ast).<br/>  Since 1.4.0, this character value can also be `<` (neighbour depth is
        /// lower), `>` (neighbour depth is greater) or `o` (levels overlap and share the same world
        /// depth).
        /// </summary>
        [DataMember(Name = "dir")]
        public string Dir { get; set; }

        /// <summary>
        /// Neighbour Instance Identifier
        /// </summary>
        [DataMember(Name = "levelIid")]
        public string LevelIid { get; set; }

        /// <summary>
        /// **WARNING**: this deprecated value is no longer exported since version 1.2.0  Replaced
        /// by: `levelIid`
        /// </summary>
        [DataMember(Name = "levelUid")]
        public int? LevelUid { get; set; }
    }
}