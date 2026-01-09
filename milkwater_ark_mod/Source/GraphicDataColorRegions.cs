using Verse;

namespace Milkwaters_ArkMod
{
    public class GraphicDataColorRegionsDef : Def
    {
        public string texPath;
        public ColorRegionTintData colorRegionTintData;
    }

    public class GraphicData_ColorRegions : GraphicData
    {
        public GraphicDataColorRegionsDef template;
        public ColorRegionTintData colorRegionTintOverride;
    }

    public class ColorRegionExtension : DefModExtension
    {
        public GraphicDataColorRegionsDef template;
        public ColorRegionTintData overrideTint;
    }

}
