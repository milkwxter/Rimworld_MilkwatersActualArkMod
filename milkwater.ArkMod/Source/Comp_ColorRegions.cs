using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace Milkwaters_ArkMod
{
    public class CompColorRegions : ThingComp
    {
        // dictionary maps each color to a region string, hooray
        public Dictionary<string, Color> regionColors = new Dictionary<string, Color>();

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            if (regionColors == null)
                regionColors = new Dictionary<string, Color>();
        }
    }
}