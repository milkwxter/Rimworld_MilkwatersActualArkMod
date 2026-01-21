using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace Milkwaters_ArkMod
{
    [HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
    public static class Patch_StonecuttingYield_OnlyMyRecipe
    {
        public static void Postfix(
            RecipeDef recipeDef,
            Pawn worker,
            List<Thing> ingredients,
            Thing dominantIngredient,
            IBillGiver billGiver,
            Precept_ThingStyle precept,
            ThingStyleDef style,
            int? overrideGraphicIndex,
            ref IEnumerable<Thing> __result)
        {
            if (recipeDef.defName != "Make_StoneBlocksAny_CraftingSpot")
                return;

            // convert to list so we can replace it
            var list = __result.ToList();

            for (int i = 0; i < list.Count; i++)
            {
                Thing t = list[i];

                if (t.def.thingCategories != null &&
                    t.def.thingCategories.Contains(ThingCategoryDefOf.StoneBlocks))
                {
                    // create a new thing with custom stack count
                    Thing newThing = ThingMaker.MakeThing(t.def);
                    newThing.stackCount = 5;

                    list[i] = newThing;
                }
            }

            __result = list;
        }
    }
}
