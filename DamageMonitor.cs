using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using Improved_Damage_Tooltips.Utilities;
using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;

namespace Improved_Damage_Tooltips.HarmonyPatches
{
    [HarmonyPatch]
    class ShowDamage
    {
        private static List<DamageInstance> damageInstances = new List<DamageInstance>();

        // private static GameObject Attacker;
        // private static bool shouldDisplayFloatingText = false;

        [HarmonyPatch(typeof(EndActionEvent), nameof(EndActionEvent.Send))]
        static void Postfix()
        {
            if (damageInstances.Any())
            {
                DisplayFloatingDamage();
            }
        }

        [HarmonyPatch(typeof(Physics), nameof(Physics.ProcessTakeDamage))]
        static bool Prefix(Event E, Physics __instance, out CombatState __state)
        {
            // shouldDisplayFloatingText = false;
            __state = new CombatState();
            GameObject Defender = __instance.ParentObject;
            GameObject gameObject =
                E.GetGameObjectParameter("Source")
                ?? E.GetGameObjectParameter("Attacker")
                ?? E.GetGameObjectParameter("Owner");
            GameObject gameObject2 =
                E.GetGameObjectParameter("Owner")
                ?? E.GetGameObjectParameter("Attacker")
                ?? ((gameObject != null && gameObject.IsCreature) ? gameObject : null);
            __state.defenderCell = Defender.CurrentCell;
            Statistic defenderStat = Defender.GetStat("Hitpoints");

            if (
                Defender.IsValid()
                && (
                    Defender.IsPlayer()
                    || (
                        gameObject2 != null
                        && gameObject2.IsPlayer()
                        && gameObject2.isAdjacentTo(Defender)
                    )
                    || Defender.IsVisible()
                )
            )
            {
                __state.isValidCombat = true;
                __state.Defender = Defender;
                __state.defenderStat = defenderStat;
                __state.Attacker = gameObject2;
                __state.attackerCell = gameObject2?.CurrentCell;
                if (gameObject2 != null)
                {
                    __state.isAttackerThePlayer = gameObject2.IsPlayer();
                }
                // XRL.Messages.MessageQueue.AddPlayerMessage("combat is
                // valid");
            }
            else
            {
                __state.isValidCombat = false;
            }

            return true;
        }

        [HarmonyPatch(typeof(Physics), nameof(Physics.ProcessTakeDamage))]
        static void Postfix(Event E, Physics __instance, bool __result, CombatState __state)
        {
            Damage damage = E.GetParameter("Damage") as Damage;
            string damageType = GetDamageType(damage);
            int damageAmount = damage.Amount;
            int penetrations = E.GetIntParameter("Penetrations");

            // ProcessTakeDamage returns false if damage processing shouldn't be
            // continued for any reason
            if (!__result || !__state.isValidCombat)
            {
                // if (!__result)
                // {
                // XRL.Messages.MessageQueue.AddPlayerMessage("__result is false");
                // }
                // if (!__state.isValidCombat)
                // {
                // XRL.Messages.MessageQueue.AddPlayerMessage(
                //     "combat instance is invalid: " +
                //     __instance.ParentObject.GetDisplayName()
                // );
                // }
                return;
            }
            // XRL.Messages.MessageQueue.AddPlayerMessage(
            //     "damaged name " + __instance.ParentObject.GetDisplayName()
            // );
            // XRL.Messages.MessageQueue.AddPlayerMessage(
            //     "damage after ProcessTakeDamage " + damageAmount.ToString()
            // );

            DamageInstance damageInstance = null;
            foreach (DamageInstance instance in damageInstances)
            {
                if (
                    instance.DefenderCell == __state.defenderCell
                    && instance.Type == damageType
                    && instance.Defender == __state.Defender
                )
                {
                    damageInstance = instance;
                    damageInstance.Amount += damageAmount;
                    if (penetrations > damageInstance.Penetrations)
                    {
                        damageInstance.Penetrations = penetrations;
                    }
                    break;
                }
            }
            if (damageInstance == null)
            {
                damageInstance = new DamageInstance(
                    damageAmount,
                    penetrations,
                    damageType,
                    __state.Defender,
                    __state.Attacker,
                    __state.isAttackerThePlayer,
                    __state.defenderStat,
                    __state.defenderCell,
                    __state.attackerCell
                );
                damageInstances.Add(damageInstance);
            }
        }

        [HarmonyPatch(typeof(CombatJuice), nameof(CombatJuice.floatingText))]
        [HarmonyPatch(
            new Type[]
            {
                typeof(GameObject),
                typeof(string),
                typeof(UnityEngine.Color),
                typeof(float),
                typeof(float),
                typeof(float),
                typeof(bool)
            }
        )]
        static bool Prefix()
        {
            return false;
        }

        static void Cleanup()
        {
            damageInstances.Clear();
        }

        static void DisplayFloatingDamage()
        {
            UnityEngine.Color damageColor = The.Color.Red;

            bool ShowZeroBleeding = Options.GetOption("OptionIDT_ShowZeroBleeding") == "Yes";
            bool ColorAnyNonPhysical = Options.GetOption("OptionIDT_ColorAnyNonPhysical") == "Yes";
            bool ShowPlayerPenetrations =
                Options.GetOption("OptionIDT_ShowPlayerPenetrations") == "Yes";
            bool ShowOtherPenetrations =
                Options.GetOption("OptionIDT_ShowOtherPenetrations") == "Yes";
            var groupedDamageInstances = damageInstances.GroupBy(inst => inst.Defender);

            // if (groupedDamageInstances.Count() > 1)
            // {
            //     XRL.Messages.MessageQueue.AddPlayerMessage("combat is MultiCell");
            // }
            foreach (var innerList in groupedDamageInstances)
            {
                if (innerList.Count() > 1)
                {
                    //innerList.Key is the Defender displayname
                    // XRL.Messages.MessageQueue.AddPlayerMessage(
                    //     "Should regroup these multiple damage instances: " + innerList.Key
                    // );
                }
                string damageType = "Physical";
                int highestDamageAmount = 0;
                int totalDamage = 0;
                bool isAttackerThePlayer = false;
                int highestPenetrations = 0;
                Cell defenderCell = null;
                GameObject Defender = null;
                Cell attackerCell = null;
                GameObject Attacker = null;
                Statistic stat = null;
                foreach (var damageInstance in innerList)
                {
                    int amount = damageInstance.Amount;
                    totalDamage += amount;
                    defenderCell = damageInstance.DefenderCell;
                    attackerCell = damageInstance.AttackerCell;
                    Defender = damageInstance.Defender;
                    Attacker = damageInstance.Attacker;
                    stat = damageInstance.Stat;

                    if (damageInstance.Penetrations > highestPenetrations)
                    {
                        highestPenetrations = damageInstance.Penetrations;
                        isAttackerThePlayer = damageInstance.IsAttackerThePlayer;
                    }
                    if (
                        (!ColorAnyNonPhysical && amount >= highestDamageAmount)
                        || (
                            ColorAnyNonPhysical
                            && amount >= highestDamageAmount
                            && damageInstance.Type != "Physical"
                        )
                    )
                    {
                        highestDamageAmount = amount;
                        damageType = damageInstance.Type;
                        damageColor = damageInstance.Color;
                    }
                }

                if (!ShowZeroBleeding && totalDamage == 0)
                {
                    continue;
                }

                float scale = GetScale(stat, totalDamage);
                // Display a smaller tooltip for some continuous damage types
                if (
                    damageType == "Bleeding"
                    || damageType == "Drain"
                    || damageType == "Poison"
                    || damageType == "Asphyxiation"
                )
                {
                    scale /= 2;
                }
                // XRL.Messages.MessageQueue.AddPlayerMessage(
                //     "display floating of type: " + damageType
                // );
                // XRL.Messages.MessageQueue.AddPlayerMessage(
                //     "total damage = " + totalDamage.ToString()
                // );

                if (
                    attackerCell != null
                    && highestPenetrations > 0
                    && (
                        (isAttackerThePlayer && ShowPlayerPenetrations)
                        || (!isAttackerThePlayer && ShowOtherPenetrations)
                    )
                )
                {
                    int penetrationThreshold = highestPenetrations;
                    if (highestPenetrations > 20)
                    {
                        penetrationThreshold = 20;
                    }
                    else if (highestPenetrations > 10)
                    {
                        penetrationThreshold = 10;
                    }
                    else if (highestPenetrations > 5)
                    {
                        penetrationThreshold = 5;
                    }
                    UnityEngine.Color penetrationColor = PenetrationsColors.Colors[
                        penetrationThreshold
                    ];
                    CombatJuice.floatingText(
                        attackerCell,
                        "x" + highestPenetrations,
                        penetrationColor,
                        1.5f,
                        14f,
                        0.40f,
                        true,
                        Attacker
                    );
                }

                CombatJuice.floatingText(
                    defenderCell,
                    "-" + totalDamage,
                    damageColor,
                    1f,
                    24f,
                    scale,
                    true,
                    Defender
                );
            }
            Cleanup();
        }

        static string GetDamageType(Damage damage)
        {
            string damageType;

            if (damage.IsHeatDamage())
            {
                if (damage.HasAttribute("NoBurn"))
                {
                    damageType = "Heat";
                }
                else
                {
                    damageType = "Fire";
                }
            }
            else if (damage.HasAttribute("Vaporized"))
            {
                damageType = "Vaporized";
            }
            else if (damage.IsColdDamage())
            {
                damageType = "Cold";
            }
            else if (damage.IsElectricDamage())
            {
                damageType = "Electric";
            }
            else if (damage.IsAcidDamage())
            {
                damageType = "Acid";
            }
            else if (damage.IsDisintegrationDamage())
            {
                damageType = "Disintegration";
            }
            else if (damage.HasAttribute("Plasma"))
            {
                damageType = "Plasma";
            }
            else if (damage.HasAttribute("Laser"))
            {
                damageType = "Light";
            }
            else if (damage.HasAttribute("Light"))
            {
                damageType = "Light";
            }
            else if (damage.HasAttribute("Poison"))
            {
                damageType = "Poison";
            }
            else if (damage.HasAttribute("Bleeding"))
            {
                damageType = "Bleeding";
            }
            else if (damage.HasAttribute("Asphyxiation"))
            {
                damageType = "Asphyxiation";
            }
            else if (damage.HasAttribute("Metabolic"))
            {
                damageType = "Metabolic";
            }
            else if (damage.HasAttribute("Drain"))
            {
                damageType = "Drain";
            }
            else if (damage.HasAttribute("Psionic"))
            {
                damageType = "Psionic";
            }
            else if (damage.HasAttribute("Mental"))
            {
                damageType = "Mental";
            }
            else
            {
                damageType = "Physical";
            }
            if (damage.HasAttribute("Illusion"))
            {
                damageType = "Illusion";
            }
            if (damage.HasAttribute("Neutron"))
            {
                damageType = "Neutron";
            }

            return damageType;
        }

        static float GetScale(Statistic stat, int totalDamage)
        {
            float scale = 1f;
            if (stat?.BaseValue > 0)
            {
                float num5 = (float)totalDamage / (float)stat.BaseValue;
                if ((double)num5 >= 0.5)
                {
                    scale = 1.8f;
                }
                else if ((double)num5 >= 0.4)
                {
                    scale = 1.6f;
                }
                else if ((double)num5 >= 0.3)
                {
                    scale = 1.4f;
                }
                else if ((double)num5 >= 0.2)
                {
                    scale = 1.2f;
                }
                else if ((double)num5 >= 0.1)
                {
                    scale = 1.1f;
                }
            }
            return scale;
        }
    }
}
