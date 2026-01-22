using RimWorld;
using System.Collections.Generic;
using Verse;
using Verse.AI;
using Verse.Noise;

namespace Milkwaters_ArkMod
{
    public class JobDriver_TameTorpor : JobDriver
    {
        private Pawn TargetPawn => (Pawn)job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(TargetPawn, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
            this.FailOn(() => !TargetPawn.health.hediffSet.HasHediff(HediffDef.Named("m_TorporSleep")));

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

            var tame = new Toil();
            tame.initAction = () =>
            {
                // special effects
                FleckMaker.Static(TargetPawn.Position, TargetPawn.Map, FleckDefOf.PsycastAreaEffect);
                LifeStageUtility.PlayNearestLifestageSound(
                    TargetPawn,
                    ls => ls.soundCall,
                    gene => gene.soundCall,
                    mutant => mutant.soundCall,
                    1f
                );

                // tame the beast
                TargetPawn.SetFaction(Faction.OfPlayer);

                // remove the torpor sleep
                Hediff torporSleep = TargetPawn.health.hediffSet.GetFirstHediffOfDef(HediffDef.Named("m_TorporSleep"));
                if (torporSleep != null)
                    TargetPawn.health.RemoveHediff(torporSleep);

                // notify player
                Find.LetterStack.ReceiveLetter(
                    "Torpor tame success",
                    $"{pawn.LabelShortCap} successfully torpor tamed {TargetPawn.LabelShortCap}.",
                    LetterDefOf.PositiveEvent,
                    TargetPawn
                );

                // tell toil we are done i think
                ReadyForNextToil();
            };
            tame.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return tame;
        }
    }
}