using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace Milkwaters_ArkMod
{
    public class CompColorRegions : ThingComp
    {
        public List<Color> regionColors;

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            if (regionColors == null)
                regionColors = new List<Color>();
        }
    }
}