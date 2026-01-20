using System.Collections.Generic;
using Verse;
using UnityEngine;

namespace Milkwaters_ArkMod
{
    public class CompColorRegions : ThingComp
    {
        // dictionary maps each color to a region string, hooray
        public Dictionary<string, Color> regionColors = new Dictionary<string, Color>();

        // cache the materials to stop my awesome memory leak, but dont write them to saves lol
        [Unsaved]
        public Dictionary<int, Material> materialCache = new Dictionary<int, Material>();

        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);

            // safety check
            if (regionColors == null)
                regionColors = new Dictionary<string, Color>();
        }
    }
}