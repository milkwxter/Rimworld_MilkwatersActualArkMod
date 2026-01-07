using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Milkwaters_ArkMod
{
    public class CompProperties_ColorRegions : CompProperties
    {
        public CompProperties_ColorRegions()
        {
            compClass = typeof(CompColorRegions);
        }
    }

    public class ColorRegionTintColorDef : Def
    {
        public Color color;
    }

    public class MaskEntry
    {
        public string regionId;
        public string maskTexPath;
        public List<string> allowedColors;
    }

    public class FacingMaskSet
    {
        public List<MaskEntry> masks;
    }

    public class ColorRegionTintData
    {
        public FacingMaskSet North;
        public FacingMaskSet East;
        public FacingMaskSet South;
        public FacingMaskSet West;
    }
}
