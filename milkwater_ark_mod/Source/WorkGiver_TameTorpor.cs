using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace Milkwaters_ArkMod
{
    public class WorkGiver_TameTorpor : WorkGiver_Scanner
    {
        public override PathEndMode PathEndMode => PathEndMode.Touch;

        public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
        {
            foreach (Pawn p in pawn.Map.mapPawns.AllPawnsSpawned)
            {
                if (p.RaceProps.Animal &&
                    p.health.hediffSet.HasHediff(HediffDef.Named("m_TorporSleep")))
                {
                    yield return p;
                }
            }
        }

        public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            if (!forced) return false;

            Pawn target = t as Pawn;
            if (target == null) return false;
            if (!pawn.IsColonistPlayerControlled) return false;
            if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly)) return false;
            if (!target.Downed) return false;
            if (!target.health.hediffSet.HasHediff(HediffDef.Named("m_TorporSleep"))) return false;

            return true;
        }

        public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
        {
            return JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("m_TameTorpor"), t);
        }
    }
}