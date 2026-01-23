using RimWorld;
using UnityEngine;
using Verse;

namespace Milkwaters_ArkMod
{
    public class DamageWorker_Torpor : DamageWorker
    {
        public override DamageResult Apply(DamageInfo dinfo, Thing thing)
        {
            DamageResult result = new DamageResult();

            if (thing is Pawn pawn)
            {
                // get torpor from xml
                float baseAmount = dinfo.Amount;

                // scale by body size
                float scaled = baseAmount / pawn.BodySize;

                // create hediff
                Hediff hediff = HediffMaker.MakeHediff(dinfo.Def.hediff, pawn);
                hediff.Severity = scaled;

                pawn.health.AddHediff(hediff, null, dinfo);

                // special effects
                float percent = scaled * 100f;
                MoteMaker.ThrowText(pawn.DrawPos, pawn.Map, $"+{percent:0.#}% torpor", new Color(0.45f, 0.85f, 0.35f));
            }

            return result;
        }
    }

}
