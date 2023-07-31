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
        private static int highestPenetrations = 0;
        private static List<DamageInstance> damageInstances = new List<DamageInstance>();
        private static GameObject Defender;
        private static Cell floatingTextLastCell = null;
        private static Statistic lastStat;

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
            Defender = __instance.ParentObject;
            GameObject gameObject =
                E.GetGameObjectParameter("Source")
                ?? E.GetGameObjectParameter("Attacker")
                ?? E.GetGameObjectParameter("Owner");
            GameObject gameObject2 =
                E.GetGameObjectParameter("Owner")
                ?? E.GetGameObjectParameter("Attacker")
                ?? ((gameObject != null && gameObject.IsCreature) ? gameObject : null);
            floatingTextLastCell = Defender.CurrentCell;
            lastStat = Defender.GetStat("Hitpoints");

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
                // XRL.Messages.MessageQueue.AddPlayerMessage("combat is valid");
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

            // ProcessTakeDamage returns false if damage processing shouldn't be
            // continued for any reason
            if (!__result || !__state.isValidCombat)
            {
                // /* Can happen if a special weapon kills with its extra damage
                //  before even touching with its regular damage (like can
                //  happen a lot with weapons that have elemental mods) */
                // if (!__state.isValidCombat && damageInstances.Any())
                // {
                //     // HandleInvalidExistingCombat(damageType);
                //     DisplayFloatingDamage();
                // }
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
                /* Detect multicell damage (explosions/gases etc)
                Multicell damage has a single damagetype (there is no "multitype"
                gases etc), so if damage of a given type is being applied to a
                different cell or different Defender than already targeted this turn,
                it is multicell damage (damage that is part of a larger explosion/gas).
                */
                if (
                    instance.Type == damageType
                    && (
                        instance.Cell != floatingTextLastCell
                        || (instance.Cell == floatingTextLastCell && instance.Defender != Defender)
                    )
                )
                {
                    // __state.isMultiCellDamage = true;
                    // Never display invalid multicell damage instances
                    if (!__state.isValidCombat)
                    {
                        // XRL.Messages.MessageQueue.AddPlayerMessage(
                        //     "combat instance is invalid, not added"
                        // );
                        return;
                    }
                }
                if (
                    instance.Cell == floatingTextLastCell
                    && instance.Type == damageType
                    && instance.Defender == Defender
                )
                {
                    damageInstance = instance;
                    damageInstance.Amount += damageAmount;
                    break;
                }
            }
            if (damageInstance == null)
            {
                damageInstance = new DamageInstance(
                    damageAmount,
                    Defender,
                    damageType,
                    lastStat,
                    floatingTextLastCell
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
            floatingTextLastCell = null;
            damageInstances.Clear();
        }

        static void DisplayFloatingDamage()
        {
            UnityEngine.Color damageColor = The.Color.Red;

            bool ShowZeroBleeding = Options.GetOption("OptionIDT_ShowZeroBleeding") == "Yes";
            bool ColorAnyNonPhysical = Options.GetOption("OptionIDT_ColorAnyNonPhysical") == "Yes";
            var groupedDamageInstances = damageInstances.GroupBy(inst => inst.Defender);

            if (groupedDamageInstances.Count() > 1)
            {
                XRL.Messages.MessageQueue.AddPlayerMessage("combat is MultiCell");
            }
            foreach (var innerList in groupedDamageInstances)
            {
                if (innerList.Count() > 1)
                {
                    //innerList.Key is the Defender displayname
                    XRL.Messages.MessageQueue.AddPlayerMessage(
                        "Should regroup these multiple damage instances: " + innerList.Key
                    );
                }
                string damageType = "Physical";
                int highestDamageAmount = 0;
                int totalDamage = 0;
                Cell cell = null;
                GameObject cellDefender = null;
                Statistic stat = null;
                foreach (var damageInstance in innerList)
                {
                    int amount = damageInstance.Amount;
                    totalDamage += amount;
                    cell = damageInstance.Cell;
                    cellDefender = damageInstance.Defender;
                    stat = damageInstance.Stat;
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
                XRL.Messages.MessageQueue.AddPlayerMessage(
                    "display floating of type: " + damageType
                );
                XRL.Messages.MessageQueue.AddPlayerMessage(
                    "total damage = " + totalDamage.ToString()
                );
                CombatJuice.floatingText(
                    cell,
                    "-" + totalDamage,
                    damageColor,
                    1.5f,
                    24f,
                    scale,
                    true,
                    cellDefender
                );
            }

            //     float scale = GetScale();
            //     Cell cell = floatingTextLastCell;

            //     // Get the color of the tooltip
            //     foreach (
            //         DamageInstance damageInstance in damageInstances.OrderByDescending(
            //             dmg => dmg.Amount
            //         )
            //     )
            //     {
            //         XRL.Messages.MessageQueue.AddPlayerMessage(
            //             "highlighted damage type = "
            //                 + damageInstance.Type
            //                 + ", value = "
            //                 + damageInstance.Amount.ToString()
            //         );
            //         if (
            //             Options.GetOption("OptionIDT_ColorAnyNonPhysical") == "Yes"
            //             && damageInstance.Type == "Physical"
            //         )
            //         {
            //             continue;
            //         }
            //         else
            //         {
            //             damageColor = damageInstance.Color;
            //             cell = damageInstance.Cell;
            //             break;
            //         }
            //     }
            //     XRL.Messages.MessageQueue.AddPlayerMessage("display floating");
            //     XRL.Messages.MessageQueue.AddPlayerMessage(
            //         "total damage = " + totalDamage.ToString()
            //     );
            //     if (Options.GetOption("OptionIDT_ShowZeroBleeding") == "No" && totalDamage == 0)
            //     {
            //         Cleanup();
            //         return;
            //     }
            //     CombatJuice.floatingText(
            //         cell,
            //         "-" + totalDamage,
            //         damageColor,
            //         1.5f,
            //         24f,
            //         scale,
            //         true,
            //         Defender
            //     );
            // }
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

        // static void HandleInvalidExistingCombat(string damageType)
        // {
        // damageInstances.RemoveAll(instance => instance.Type == damageType && (instance.Cell != floatingTextLastCell || (instance.Cell == floatingTextLastCell && instance.Defender != Defender)));
        // foreach (DamageInstance instance in damageInstances)
        // {
        // 	/* Detect multicell damage (explosions/gases etc)
        // 		Multicell damage has a single damagetype (there is no "multitype"
        // 		gases etc), so if damage of a given type is being applied to a
        // 		different cell or different Defender than already targeted this turn,
        // 		it is multicell damage (damage that is part of a larger explosion/gas).
        // 	*/
        // 	if (instance.Type == damageType && (instance.Cell != floatingTextLastCell || (instance.Cell == floatingTextLastCell && instance.Defender != Defender)))
        // 	{
        // 		isMultiCellDamage = true;
        // 		break;
        // 	}
        // }
        // if (damageInstances.Any())
        // {
        // 	XRL.Messages.MessageQueue.AddPlayerMessage("Removed invalid multicell instances, displaying remaining");
        // 	DisplayFloatingDamage();
        // }
        // else
        // {
        // 	XRL.Messages.MessageQueue.AddPlayerMessage("Invalid damage is MultiCellDamage, removed");
        // 	// Cleanup();
        // }
        // }
    }
}
