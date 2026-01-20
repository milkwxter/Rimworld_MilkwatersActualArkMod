using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Milkwaters_ArkMod
{
    public class CompProperties_ColorRegions : CompProperties
    {
        public CompProperties_ColorRegions()
        {
            // what comp to attach to
            compClass = typeof(CompColorRegions);
        }
    }

    public class ColorRegionDef : Def
    {
        // list of allowed colors to pick randomly from
        public List<string> allowedColors;
    }

    public class ColorRegionTintColorDef : Def
    {
        // a color bruh
        public Color color;
    }

    public class MaskEntry
    {
        // unique region id, its a string though
        public string regionId;

        // path to the mask texture
        public string maskTexPath;
    }

    public class FacingMaskSet
    {
        // all the mask entries for a single face (north, south, etc)
        public List<MaskEntry> masks;
    }

    public class ColorRegionTintData
    {
        // mask sets for each facing direction
        public FacingMaskSet North;
        public FacingMaskSet East;
        public FacingMaskSet South;
        public FacingMaskSet West;
    }
}
