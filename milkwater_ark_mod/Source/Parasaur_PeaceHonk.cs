using RimWorld;
using Verse;

namespace Milkwaters_ArkMod
{
    public class CompProperties_Ability_ParasaurPeaceHonk : CompProperties_AbilityEffect
    {
        public float radius = 30f;

        public CompProperties_Ability_ParasaurPeaceHonk()
        {
            compClass = typeof(CompAbilityEffect_ParasaurPeaceHonk);
        }
    }

    public class CompAbilityEffect_ParasaurPeaceHonk : CompAbilityEffect
    {
        public new CompProperties_Ability_ParasaurPeaceHonk Props => (CompProperties_Ability_ParasaurPeaceHonk)props;

        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            base.Apply(target, dest);

            Pawn caster = parent.pawn;
            Map map = caster.Map;

            if (map == null)
                return;

            // make him flee
            target.Pawn.mindState.mentalStateHandler.TryStartMentalState(MentalStateDefOf.Roaming);

            FleckMaker.Static(target.Pawn.Position, map, FleckDefOf.PsycastAreaEffect);
        }

        public override bool Valid(LocalTargetInfo target, bool showMessages = false)
        {
            if (target == null) return false;

            if (target.Pawn.MentalStateDef == MentalStateDefOf.Manhunter) return true;

            return false;
        }
    }
}
