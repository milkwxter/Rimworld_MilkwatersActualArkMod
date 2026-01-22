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
                MoteText mote = (MoteText)ThingMaker.MakeThing(ThingDefOf.Mote_Text);
                mote.text = $"+{percent:0.#}% torpor";
                mote.textColor = new Color(0.45f, 0.85f, 0.35f);
                mote.exactPosition = pawn.DrawPos;
                GenSpawn.Spawn(mote, pawn.Position, pawn.Map);

            }

            return result;
        }
    }

}
