using HarmonyLib;
using XRL.UI;
using XRL.World;

namespace Improved_Damage_Tooltips.HarmonyPatches
{
    [HarmonyPatch]
    public class ShowXPTooltips
    {
        public static int totalXP = 0;
        public static GameObject Player = null;

        [HarmonyPatch(typeof(AwardedXPEvent), nameof(AwardedXPEvent.Send))]
        static bool Prefix(IXPEvent ParentEvent, int Amount)
        {
            GameObject Actor = ParentEvent.Actor;
            if (Actor.IsPlayer())
            {
                totalXP += Amount;
                if (Player == null)
                {
                    Player = Actor;
                }
            }
            return true;
        }

        [HarmonyPatch(typeof(EndActionEvent), nameof(EndActionEvent.Send))]
        static void Postfix()
        {
            if (totalXP > 0 && Options.GetOption("OptionIDT_ShowXP") == "Yes")
            {
                Player.ParticleText(totalXP.ToString(), 'C');
                totalXP = 0;
            }
        }
    }
}
