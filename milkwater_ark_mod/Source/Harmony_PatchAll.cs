using HarmonyLib;
using Verse;

namespace Milkwaters_ArkMod
{
    [StaticConstructorOnStartup]
    public static class Startup
    {
        static Startup()
        {
            new Harmony("milkwaters.arkmod").PatchAll();
            Log.Message("[milkwaters.arkmod] Harmony Patchall Executed!");
        }
    }
}
