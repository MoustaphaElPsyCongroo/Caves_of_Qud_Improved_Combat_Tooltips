using HarmonyLib;
using XRL.UI;
using XRL;
using XRL.World;

namespace Gokudera_ElPsyCongroo_ICTooltips.HarmonyPatches
{
    [HasGameBasedStaticCache]
    [HarmonyPatch]
    public class ShowXPTooltips
    {
        public static int totalXP = 0;

        [GameBasedStaticCache]
        public static GameObject Player = null;

        [HarmonyPatch(typeof(AwardedXPEvent), nameof(AwardedXPEvent.Send))]
        static bool Prefix(IXPEvent ParentEvent, int Amount)
        {
            GameObject Actor = ParentEvent.Actor;

            if (Actor.IsPlayer())
            {
                totalXP += Amount;
                Player = Actor;
            }
            return true;
        }

        [HarmonyPatch(typeof(EndActionEvent), nameof(EndActionEvent.Send))]
        static void Prefix(GameObject __state)
        {
            if (totalXP > 0 && Options.GetOption("OptionICTooltips_ShowXP") == "Yes")
            {
                Player.ParticleText(totalXP.ToString(), 'C');
                totalXP = 0;
            }
        }
    }
}
