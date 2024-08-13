using HarmonyLib;
using System;
using XRL.Core;
using XRL.UI;
using XRL.World;

namespace Gokudera_ElPsyCongroo_ICTooltips.XPTooltips
{
    [HarmonyPatch]
    public class ShowXPTooltips
    {
        public static int totalXP = 0;

        [HarmonyPatch(typeof(AwardedXPEvent), nameof(AwardedXPEvent.Send))]
        static bool Prefix(IXPEvent ParentEvent, int Amount)
        {
            if (ParentEvent.Actor.IsPlayer())
            {
                totalXP += Amount;
            }
            return true;
        }

        [HarmonyPatch(typeof(GameObject), nameof(GameObject.HandleEvent))]
        [HarmonyPatch(new Type[] { typeof(MinEvent) })]
        static void Prefix(MinEvent E)
        {
            if (E.ID == EndActionEvent.ID)
            {
                if (totalXP > 0 && Options.GetOption("OptionICTooltips_ShowXP") == "Yes")
                {
                    GameObject Player = XRLCore.Core.Game.Player.Body;
                    Player.ParticleText(totalXP.ToString(), 'C');
                    totalXP = 0;
                }
            }
        }
    }
}
