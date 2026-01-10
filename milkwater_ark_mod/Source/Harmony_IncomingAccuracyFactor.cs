using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace Milkwaters_ArkMod
{
    [HarmonyPatch(typeof(ShotReport), nameof(ShotReport.AimOnTargetChance), MethodType.Getter)]
    public static class Patch_AimOnTargetChance
    {
        private static readonly AccessTools.FieldRef<ShotReport, TargetInfo> targetRef = AccessTools.FieldRefAccess<ShotReport, TargetInfo>("target");

        public static void Postfix(ref float __result, ref ShotReport __instance)
        {
            TargetInfo ti = targetRef(__instance);

            Pawn targetPawn = ti.Thing as Pawn;
            if (targetPawn == null)
                return;

            float incomingFactor = targetPawn.GetStatValue(StatDef.Named("m_IncomingAccuracyFactor"));

            __result *= incomingFactor;
        }
    }

}
