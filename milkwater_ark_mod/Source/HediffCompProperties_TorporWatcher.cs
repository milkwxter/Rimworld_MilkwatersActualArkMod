using Verse;

namespace Milkwaters_ArkMod
{
    public class HediffCompProperties_TorporWatcher : HediffCompProperties
    {
        public HediffCompProperties_TorporWatcher()
        {
            compClass = typeof(HediffComp_TorporWatcher);
        }
    }

    public class HediffComp_TorporWatcher : HediffComp
    {
        public override void CompPostTick(ref float severityAdjustment)
        {
            // check once per second
            if (!parent.pawn.IsHashIntervalTick(60))
                return;

            if (parent.Severity >= 1.0f)
            {
                Pawn pawn = parent.pawn;

                // remove torpor
                pawn.health.RemoveHediff(parent);

                // add torpor induced sleep instead
                HediffDef sleepDef = HediffDef.Named("m_TorporSleep");
                Hediff sleep = HediffMaker.MakeHediff(sleepDef, pawn);
                sleep.Severity = 1.0f;

                pawn.health.AddHediff(sleep);
            }
        }
    }
}