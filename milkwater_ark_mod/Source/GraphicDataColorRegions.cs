using Verse;

namespace Milkwaters_ArkMod
{
    public class GraphicDataColorRegionsDef : Def
    {
        // base texture path for pawn
        public string texPath;

        // mask sets for each facing direction
        public ColorRegionTintData colorRegionTintData;
    }

    public class GraphicData_ColorRegions : GraphicData
    {
        // references the template made in xml
        public GraphicDataColorRegionsDef template;

        // mask sets for each facing direction
        public ColorRegionTintData colorRegionTintOverride;
    }
}
