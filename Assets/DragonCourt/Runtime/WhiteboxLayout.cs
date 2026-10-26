using System;
using UnityEngine;

namespace DragonCourt
{
    [Serializable] public class WhiteboxLayout
    {
        public BlockSpec[] blocks;
        public RampSpec[] ramps;
    }
    [Serializable] public class BlockSpec
    {
        public string name;
        public string kind;
        public float[] center;
        public float[] size;
    }
    // Ramps run along world Z. Height is the walking surface at each end.
    [Serializable] public class RampSpec
    {
        public string name;
        public float x, width, z0, z1, y0, y1;
    }
}
