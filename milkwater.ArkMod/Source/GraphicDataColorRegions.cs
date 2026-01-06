using Verse;

namespace Milkwaters_ArkMod
{
    // Library of reusable color-region setups, one per dino "family"
    public class GraphicDataColorRegionsDef : Def
    {
        public string texPath;                    // default texPath for this template
        public ColorRegionTintData colorRegionTintData;
    }

    // The GraphicData actually used on PawnKinds / ThingDefs
    public class GraphicData_ColorRegions : GraphicData
    {
        // Reference to the template def by defName in XML
        public GraphicDataColorRegionsDef template;

        // Optional: per‑use overrides if you ever want them
        public ColorRegionTintData colorRegionTintOverride;
    }

    public class ColorRegionExtension : DefModExtension
    {
        public GraphicDataColorRegionsDef template;
        public ColorRegionTintData overrideTint;
    }

}
