using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace Milkwaters_ArkMod
{
    public class CompProperties_Ability_ParasaurRadarScan : CompProperties_AbilityEffect
    {
        public float radius = 30f;

        public CompProperties_Ability_ParasaurRadarScan()
        {
            compClass = typeof(CompAbilityEffect_ParasaurRadarScan);
        }
    }

    public class CompAbilityEffect_ParasaurRadarScan : CompAbilityEffect
    {
        public new CompProperties_Ability_ParasaurRadarScan Props => (CompProperties_Ability_ParasaurRadarScan)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            Map map = caster.Map;

            if (map == null)
                return;

            // collect detected hostiles
            List<Pawn> detected = new List<Pawn>();

            foreach (IntVec3 cell in GenRadial.RadialCellsAround(caster.Position, Props.radius, true))
            {
                if (!cell.InBounds(map))
                    continue;

                Pawn pawn = cell.GetFirstPawn(map);
                if (pawn == null)
                    continue;

                bool isHostileFaction = pawn.Faction != null && pawn.Faction.HostileTo(caster.Faction);

                if (pawn.HostileTo(caster) || isHostileFaction)
                {
                    detected.Add(pawn);
                    HighlightPawn(pawn, map);
                }
            }

            // feedback
            if (detected.Count > 0)
            {
                Messages.Message(
                    $"Radar scan detected {detected.Count} hostile creatures.",
                    new LookTargets(detected),
                    MessageTypeDefOf.NeutralEvent
                );
            }
            else
            {
                Messages.Message(
                    "Radar scan detected no hostiles.",
                    caster,
                    MessageTypeDefOf.NeutralEvent
                );
            }
        }

        private void HighlightPawn(Pawn pawn, Map map)
        {
            // do a fleck
            FleckMaker.AttachedOverlay(pawn, DefDatabase<FleckDef>.GetNamed("m_RadarScan_Hostile_Fleck"), new Vector3(0f, 0f, 0f));
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = false)
        {
            return true;
        }
    }
}
