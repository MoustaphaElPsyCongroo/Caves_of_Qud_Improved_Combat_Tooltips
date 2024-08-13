using HarmonyLib;
using System;
using System.Linq;
using System.Collections.Generic;
using Gokudera_ElPsyCongroo_ICTooltips.Utilities;
using XRL;
using XRL.UI;
using XRL.World;
using XRL.World.Parts;
using XRL.Rules;

namespace Gokudera_ElPsyCongroo_ICTooltips.DamageMonitor
{
    [HarmonyPatch]
    class DamageMonitor
    {
        private static List<DamageInstance> damageInstances = new List<DamageInstance>();
        private static int lastDefenderDV;
        private static int lastAttackerRollAgainstDV;
        private static int lastDefenderAV;
        private static int lastAttackerBestRollAgainstAV;
        private static int lastMissilePenetrations;
        private static GameObject lastMissileAttacker;
        private static bool tookDamage = false;
        private static bool isMissileAttack = false;
        private static bool missedMissileAttack = false;

        [HarmonyPatch(typeof(GameObject), nameof(GameObject.HandleEvent))]
        [HarmonyPatch(new Type[] { typeof(MinEvent) })]
        static void Postfix(MinEvent E)
        {
            if (E.ID == EndActionEvent.ID)
            {
                if (damageInstances.Any())
                {
                    if (tookDamage)
                    {
                        DisplayFloatingDamage();
                    }

                    if (!tookDamage || missedMissileAttack)
                    {
                        DisplayFloatingRolls();
                    }

                    tookDamage = false;
                    isMissileAttack = false;
                    missedMissileAttack = false;
                }
            }
        }

        // to get the latest attackerRoll and defenderRoll before damage or
        // miss or etc is processed
        [HarmonyPatch(typeof(GameObject), nameof(GameObject.FireEvent))]
        [HarmonyPatch(new Type[] { typeof(Event) })]
        static void Postfix(Event E)
        {
            // Before melee attack/any attack that checks DV
            if (E.ID == "WeaponGetDefenderDV")
            {
                lastAttackerRollAgainstDV = E.GetIntParameter("Result");
                lastDefenderDV = E.GetIntParameter("DV");

                if (isMissileAttack)
                {
                    // XRL.Messages.MessageQueue.AddPlayerMessage("performing missile attack");

                    int incomingAttackerRollAgainstDV = lastAttackerRollAgainstDV;
                    string damageType = "MissileMiss";

                    if (incomingAttackerRollAgainstDV <= lastDefenderDV)
                    {
                        // XRL.Messages.MessageQueue.AddPlayerMessage("missed missile attack");

                        missedMissileAttack = true;

                        int damageAmount = 0;
                        int penetrations = 0;
                        GameObject Defender = E.GetGameObjectParameter("Defender");
                        GameObject Attacker = lastMissileAttacker;
                        Cell DefenderCell = Defender.CurrentCell;
                        Statistic DefenderStat = Defender.GetStat("Hitpoints");
                        Cell AttackerCell = Attacker.CurrentCell;
                        bool isDefenderTakingDamage = false;
                        bool isAttackerThePlayer = false;

                        if (Attacker != null)
                        {
                            isAttackerThePlayer = Attacker.IsPlayer();
                        }

                        DamageInstance damageInstance = null;
                        foreach (DamageInstance instance in damageInstances)
                        {
                            if (
                                instance.DefenderCell == DefenderCell
                                && instance.Type == "MissileMiss"
                                && instance.Defender == Defender
                            )
                            {
                                damageInstance = instance;
                                if (incomingAttackerRollAgainstDV > instance.AttackerRollAgainstDV)
                                {
                                    damageInstance.AttackerRollAgainstDV = incomingAttackerRollAgainstDV;
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
                                lastDefenderAV,
                                lastAttackerBestRollAgainstAV,
                                lastAttackerRollAgainstDV,
                                Defender,
                                Attacker,
                                isAttackerThePlayer,
                                isDefenderTakingDamage,
                                DefenderStat,
                                DefenderCell,
                                AttackerCell
                            );
                            damageInstances.Add(damageInstance);
                        }
                    }
                }
            }

            // After Melee miss
            if (E.ID == "DefenderAfterAttackMissed")
            {
                // XRL.Messages.MessageQueue.AddPlayerMessage("missed");
                if (!tookDamage)
                {
                    int incomingAttackerRollAgainstDV = lastAttackerRollAgainstDV;
                    string damageType = "Miss";
                    int damageAmount = 0;
                    int penetrations = 0;
                    GameObject Defender = E.GetGameObjectParameter("Defender");
                    GameObject Attacker = E.GetGameObjectParameter("Attacker");
                    Cell DefenderCell = Defender.CurrentCell;
                    Statistic DefenderStat = Defender.GetStat("Hitpoints");
                    Cell AttackerCell = Attacker.CurrentCell;
                    bool isDefenderTakingDamage = false;
                    bool isAttackerThePlayer = false;

                    if (Attacker != null)
                    {
                        isAttackerThePlayer = Attacker.IsPlayer();
                    }

                    DamageInstance damageInstance = null;
                    foreach (DamageInstance instance in damageInstances)
                    {
                        if (instance.DefenderCell == DefenderCell && instance.Type == "Miss" && instance.Defender == Defender)
                        {
                            damageInstance = instance;
                            if (incomingAttackerRollAgainstDV > instance.AttackerRollAgainstDV)
                            {
                                damageInstance.AttackerRollAgainstDV = incomingAttackerRollAgainstDV;
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
                            lastDefenderAV,
                            lastAttackerBestRollAgainstAV,
                            lastAttackerRollAgainstDV,
                            Defender,
                            Attacker,
                            isAttackerThePlayer,
                            isDefenderTakingDamage,
                            DefenderStat,
                            DefenderCell,
                            AttackerCell
                        );
                        damageInstances.Add(damageInstance);
                    }
                }
            }

            // After Melee hit
            if (E.ID == "DefenderHit")
            {
                // if penetrations <= 0, "you fail to penetrate armor"
                if (E.GetIntParameter("Penetrations") <= 0)
                {
                    if (!tookDamage)
                    {
                        int incomingAttackerRollAgainstAV = lastAttackerBestRollAgainstAV;
                        string damageType = "NoPen";
                        int damageAmount = 0;
                        int penetrations = 0;
                        GameObject Defender = E.GetGameObjectParameter("Defender");
                        GameObject Attacker = E.GetGameObjectParameter("Attacker");
                        Cell DefenderCell = Defender.CurrentCell;
                        Statistic DefenderStat = Defender.GetStat("Hitpoints");
                        Cell AttackerCell = Attacker.CurrentCell;
                        bool isDefenderTakingDamage = false;
                        bool isAttackerThePlayer = false;

                        if (Attacker != null)
                        {
                            isAttackerThePlayer = Attacker.IsPlayer();
                        }

                        DamageInstance damageInstance = null;
                        foreach (DamageInstance instance in damageInstances)
                        {
                            if (
                                instance.DefenderCell == DefenderCell && instance.Type == "NoPen" && instance.Defender == Defender
                            )
                            {
                                damageInstance = instance;
                                if (incomingAttackerRollAgainstAV > instance.AttackerBestRollAgainstAV)
                                {
                                    damageInstance.AttackerBestRollAgainstAV = incomingAttackerRollAgainstAV;
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
                                lastDefenderAV,
                                lastAttackerBestRollAgainstAV,
                                lastAttackerRollAgainstDV,
                                Defender,
                                Attacker,
                                isAttackerThePlayer,
                                isDefenderTakingDamage,
                                DefenderStat,
                                DefenderCell,
                                AttackerCell
                            );
                            damageInstances.Add(damageInstance);
                        }
                    }
                }
            }

            if (E.ID == "ProjectileEnteringCell")
            {
                // XRL.Messages.MessageQueue.AddPlayerMessage("projectile entering cell");
                isMissileAttack = true;
                lastMissileAttacker = E.GetGameObjectParameter("Attacker");
            }

            // After Ranged hit
            if (E.ID == "DefenderProjectileHit")
            {
                // Critical missile or missile autohit. Since we'd already have
                // addded a missilemiss instance in case of roll < DV, we need
                // to remove it. Not adding it in the first place would be
                // better but less efficient (we'd need to check if critical
                // hit or autohit or other DV bypass)
                if (damageInstances.LastOrDefault()?.Type == "MissileMiss")
                {
                    // XRL.Messages.MessageQueue.AddPlayerMessage("critical/autohit so removed missileMiss instance");
                    damageInstances.RemoveAt(damageInstances.Count - 1);
                    missedMissileAttack = false;
                }

                lastMissilePenetrations = E.GetIntParameter("Penetrations");
                if (E.GetIntParameter("Penetrations") <= 0)
                {
                    if (!tookDamage)
                    {
                        int incomingAttackerRollAgainstAV = lastAttackerBestRollAgainstAV;
                        string damageType = "NoPen";
                        int damageAmount = 0;
                        int penetrations = 0;
                        GameObject Defender = E.GetGameObjectParameter("Defender");
                        GameObject Attacker =
                            E.GetGameObjectParameter("Owner")
                            ?? E.GetGameObjectParameter("Attacker")
                            ?? E.GetGameObjectParameter("Source")
                            ?? lastMissileAttacker;

                        Cell DefenderCell = Defender.GetCurrentCell();
                        Statistic DefenderStat = Defender.GetStat("Hitpoints");
                        Cell AttackerCell = Attacker.GetCurrentCell();
                        bool isDefenderTakingDamage = false;
                        bool isAttackerThePlayer = false;

                        if (Attacker != null)
                        {
                            isAttackerThePlayer = Attacker.IsPlayer();
                        }

                        DamageInstance damageInstance = null;
                        foreach (DamageInstance instance in damageInstances)
                        {
                            if (
                                instance.DefenderCell == DefenderCell && instance.Type == "NoPen" && instance.Defender == Defender
                            )
                            {
                                damageInstance = instance;
                                if (incomingAttackerRollAgainstAV > instance.AttackerBestRollAgainstAV)
                                {
                                    damageInstance.AttackerBestRollAgainstAV = incomingAttackerRollAgainstAV;
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
                                lastDefenderAV,
                                lastAttackerBestRollAgainstAV,
                                lastAttackerRollAgainstDV,
                                Defender,
                                Attacker,
                                isAttackerThePlayer,
                                isDefenderTakingDamage,
                                DefenderStat,
                                DefenderCell,
                                AttackerCell
                            );
                            damageInstances.Add(damageInstance);
                        }
                    }
                }
                else
                {
                    if (damageInstances.LastOrDefault() != null)
                    {
                        damageInstances.LastOrDefault().Penetrations = E.GetIntParameter("Penetrations");
                    }
                }
            }
        }

        // If Defender dies DefenderCell ceases to exist after
        // ProcessTakeDamage, so I save it before.
        [HarmonyPatch(typeof(Physics), nameof(Physics.ProcessTakeDamage))]
        static void Prefix(Event E, Physics __instance, out CombatState __state)
        {
            __state = new CombatState();

            GameObject Defender = __instance.ParentObject;
            GameObject Attacker =
                E.GetGameObjectParameter("Owner") ?? E.GetGameObjectParameter("Attacker") ?? E.GetGameObjectParameter("Source");

            if (isMissileAttack && Attacker == null)
            {
                Attacker = lastMissileAttacker;
            }

            // Permitts adding damage instance for already dead creatures, for
            // example creature killed in the middle of a multi attack
            if (
                Defender.IsValid()
                && (
                    Defender.IsPlayer()
                    || (Attacker != null && Attacker.IsPlayer() && Attacker.isAdjacentTo(Defender))
                    || Defender.IsVisible()
                )
            )
            {
                __state.isValidCombat = true;
                __state.Defender = Defender;
                __state.Attacker = Attacker;
                __state.DefenderCell = Defender.GetCurrentCell();
                __state.DefenderStat = Defender.GetStat("Hitpoints");
                __state.AttackerCell = Attacker?.GetCurrentCell();

                if (Attacker != null)
                {
                    __state.isAttackerThePlayer = Attacker.IsPlayer();
                }
            }
            else
            {
                __state.isValidCombat = false;
            }
        }

        [HarmonyPatch(typeof(Physics), nameof(Physics.ProcessTakeDamage))]
        static void Postfix(Event E, Physics __instance, bool __result, CombatState __state)
        {
            // Invalid combat. ProcessTakeDamage returns false if damage
            // processing shouldn't be continued for any reason
            if (__result != false && __state.isValidCombat == true)
            {
                tookDamage = true;

                Damage damage = E.GetParameter("Damage") as Damage;
                string damageType = GetDamageType(damage);
                int damageAmount = damage.Amount;
                int penetrations = E.GetIntParameter("Penetrations");
                bool isDefenderTakingDamage = true;

                //Regroup damage instances by type. We need only one per type,
                //we'll group the types into one floating tooltip later
                //(in DisplayFloatingDamage())
                DamageInstance damageInstance = null;
                foreach (DamageInstance instance in damageInstances)
                {
                    if (
                        instance.DefenderCell == __state.DefenderCell
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
                        lastDefenderAV,
                        lastAttackerBestRollAgainstAV,
                        lastAttackerRollAgainstDV,
                        __state.Defender,
                        __state.Attacker,
                        __state.isAttackerThePlayer,
                        isDefenderTakingDamage,
                        __state.DefenderStat,
                        __state.DefenderCell,
                        __state.AttackerCell
                    );
                    damageInstances.Add(damageInstance);
                }
            }
        }

        // (DIRTY) Copy of the original RollDamagePenetrations method. Edited
        // to retrieve the highest pen roll.
        // Dirty way to do it but since the value is not accessible outside of
        // here it's easier than a transpiler patch.
        [HarmonyPatch(typeof(Stat), nameof(Stat.RollDamagePenetrations))]
        static bool Prefix(int TargetInclusive, int Bonus, int MaxBonus, ref int __result)
        {
            int num = 0;
            int num2 = 3;
            int AV = TargetInclusive;
            int bestRollAgainstAV = 0;
            bool debugDamagePenetrations = Options.DebugDamagePenetrations;
            while (num2 == 3)
            {
                num2 = 0;
                for (int i = 0; i < 3; i++)
                {
                    int num3 = Stat.Random(1, 10) - 2;
                    int num4 = 0;
                    while (num3 == 8)
                    {
                        num4 += 8;
                        num3 = Stat.Random(1, 10) - 2;
                    }
                    num4 += num3;
                    int num5 = num4 + Math.Min(Bonus, MaxBonus);
                    bestRollAgainstAV = Math.Max(num5, bestRollAgainstAV);
                    if (num5 > TargetInclusive)
                    {
                        if (debugDamagePenetrations)
                        {
                            XRL.Messages.MessageQueue.AddPlayerMessage("Penned with Roll:" + num4 + " Final:" + num5);
                        }
                        num2++;
                    }
                    else if (debugDamagePenetrations)
                    {
                        XRL.Messages.MessageQueue.AddPlayerMessage("Didn't pen with " + num4 + " Final:" + num5);
                    }
                }
                if (debugDamagePenetrations)
                {
                    XRL.Messages.MessageQueue.AddPlayerMessage(
                        "{{K|Penning Bonus: "
                            + Bonus
                            + " Max: "
                            + MaxBonus
                            + " Used: "
                            + Math.Min(Bonus, MaxBonus)
                            + " Target: "
                            + TargetInclusive
                            + "(Penned "
                            + num2
                            + " times)}}"
                    );
                }
                if (num2 >= 1)
                {
                    num++;
                }
                Bonus -= 2;
            }
            lastAttackerBestRollAgainstAV = bestRollAgainstAV;
            lastDefenderAV = AV;
            __result = num;
            return false;
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

            bool ShowZeroBleeding = Options.GetOption("OptionICTooltips_ShowZeroBleeding") == "Yes";
            bool ColorAnyNonPhysical = Options.GetOption("OptionICTooltips_ColorAnyNonPhysical") == "Yes";
            bool ShowPlayerPenetrations = Options.GetOption("OptionICTooltips_ShowPlayerPenetrations") == "Yes";
            bool ShowOtherPenetrations = Options.GetOption("OptionICTooltips_ShowOtherPenetrations") == "Yes";
            bool ShowPlayerPenetrationRolls = Options.GetOption("OptionICTooltips_ShowPlayerPenetrationRolls") == "Yes";
            bool ShowOtherPenetrationRolls = Options.GetOption("OptionICTooltips_ShowOtherPenetrationRolls") == "Yes";
            var groupedDamageInstances = damageInstances.GroupBy(inst => inst.Defender);

            // if (groupedDamageInstances.Count() > 1)
            // {
            //     XRL.Messages.MessageQueue.AddPlayerMessage("combat is MultiCell");
            // }
            foreach (var innerList in groupedDamageInstances)
            {
                // if (innerList.Count() > 1)
                // {
                //innerList.Key is the Defender displayname
                //     XRL.Messages.MessageQueue.AddPlayerMessage(
                //         "Should regroup these multiple damage instances: " + innerList.Key
                //     );
                // }
                string damageType = "Physical";
                int highestDamageAmount = 0;
                int totalDamage = 0;
                bool isAttackerThePlayer = false;
                int highestPenetrations = 0;
                int highestPenDefenderAV = 0;
                int highestPenAttackerRoll = 0;
                Cell DefenderCell = null;
                GameObject Defender = null;
                Cell AttackerCell = null;
                GameObject Attacker = null;
                Statistic stat = null;
                foreach (var damageInstance in innerList)
                {
                    if (damageInstance.Type == "Miss" || damageInstance.Type == "NoPen" || damageInstance.Type == "MissileMiss")
                    {
                        continue;
                    }

                    int amount = damageInstance.Amount;
                    totalDamage += amount;
                    DefenderCell = damageInstance.DefenderCell;
                    AttackerCell = damageInstance.AttackerCell;
                    Defender = damageInstance.Defender;
                    Attacker = damageInstance.Attacker;
                    stat = damageInstance.Stat;
                    isAttackerThePlayer = damageInstance.IsAttackerThePlayer;

                    if (damageInstance.Penetrations > highestPenetrations)
                    {
                        highestPenetrations = damageInstance.Penetrations;
                        highestPenDefenderAV = damageInstance.DefenderAV;
                        highestPenAttackerRoll = damageInstance.AttackerBestRollAgainstAV;
                    }
                    if (
                        (!ColorAnyNonPhysical && amount >= highestDamageAmount)
                        || (ColorAnyNonPhysical && amount >= highestDamageAmount && damageInstance.Type != "Physical")
                    )
                    {
                        // Prevent Physical from overriding special damage
                        // color when it has the same amount as special damage
                        if (amount == highestDamageAmount && damageInstance.Type == "Physical")
                        {
                            continue;
                        }
                        highestDamageAmount = amount;
                        damageColor = damageInstance.Color;
                        damageType = damageInstance.Type;
                    }
                }

                if (!ShowZeroBleeding && totalDamage == 0)
                {
                    continue;
                }

                float scale = GetScale(stat, totalDamage);
                // Display a smaller tooltip for some continuous damage types
                if (damageType == "Bleeding" || damageType == "Drain" || damageType == "Poison" || damageType == "Asphyxiation")
                {
                    scale /= 2;
                }

                if (
                    AttackerCell != null
                    && highestPenetrations > 0
                    && ((isAttackerThePlayer && ShowPlayerPenetrations) || (!isAttackerThePlayer && ShowOtherPenetrations))
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
                    UnityEngine.Color penetrationColor = PenetrationsColors.Colors[penetrationThreshold];
                    if (
                        (isAttackerThePlayer && ShowPlayerPenetrationRolls) || (!isAttackerThePlayer && ShowOtherPenetrationRolls)
                    )
                    {
                        CombatJuice.floatingText(
                            AttackerCell,
                            "x" + highestPenetrations + " - [" + highestPenAttackerRoll + " vs " + highestPenDefenderAV + "]",
                            penetrationColor,
                            1.5f,
                            14f,
                            0.40f,
                            true,
                            Attacker
                        );
                    }
                    else if (AttackerCell != null)
                    {
                        CombatJuice.floatingText(
                            AttackerCell,
                            "x" + highestPenetrations,
                            penetrationColor,
                            1.5f,
                            14f,
                            0.40f,
                            true,
                            Attacker
                        );
                    }
                }

                if (
                    AttackerCell != null
                    && highestPenetrations > 0
                    && (
                        (isAttackerThePlayer && !ShowPlayerPenetrations && ShowPlayerPenetrationRolls)
                        || (!isAttackerThePlayer && !ShowOtherPenetrations && ShowOtherPenetrationRolls)
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
                    UnityEngine.Color penetrationColor = PenetrationsColors.Colors[penetrationThreshold];

                    CombatJuice.floatingText(
                        AttackerCell,
                        "[" + highestPenAttackerRoll + " vs " + highestPenDefenderAV + "]",
                        penetrationColor,
                        1.5f,
                        14f,
                        0.40f,
                        true,
                        Attacker
                    );
                }

                CombatJuice.floatingText(DefenderCell, "-" + totalDamage, damageColor, 1f, 24f, scale, true, Defender);
            }
            Cleanup();
        }

        static void DisplayFloatingRolls()
        {
            bool ShowPlayerMissOrBlockRolls = Options.GetOption("OptionICTooltips_ShowPlayerMissOrBlockRolls") == "Yes";
            bool ShowOtherMissOrBlockRolls = Options.GetOption("OptionICTooltips_ShowOtherMissOrBlockRolls") == "Yes";

            if (!ShowPlayerMissOrBlockRolls && !ShowOtherMissOrBlockRolls)
            {
                Cleanup();
                return;
            }

            UnityEngine.Color damageColor = The.Color.Red;
            var groupedDamageInstances = damageInstances.GroupBy(inst => inst.Defender);

            foreach (var innerList in groupedDamageInstances)
            {
                string damageType = "";
                bool isAttackerThePlayer = false;
                int defenderDV = 0;
                int attackerRollAgainstDV = 0;
                int defenderAV = 0;
                int attackerRollAgainstAV = 0;
                Cell DefenderCell = null;
                GameObject Defender = null;
                Cell AttackerCell = null;
                GameObject Attacker = null;

                foreach (var damageInstance in innerList)
                {
                    // Don't display rolls for invalid combat (including non
                    // visible defenders)
                    if (
                        !(
                            damageInstance.Defender.IsValid()
                            && (
                                damageInstance.Defender.IsPlayer()
                                || (
                                    damageInstance.Attacker != null
                                    && damageInstance.Attacker.IsPlayer()
                                    && damageInstance.Attacker.isAdjacentTo(damageInstance.Defender)
                                )
                                || damageInstance.Defender.IsVisible()
                            )
                        )
                    )
                    {
                        continue;
                    }

                    if (damageInstance.Type != "Miss" && damageInstance.Type != "NoPen" && damageInstance.Type != "MissileMiss")
                    {
                        continue;
                    }

                    // In case of Miss and NoPen/MissileMiss in same action,
                    // only display NoPen, skip Misses,
                    if (damageInstance.Type == "Miss" && (damageType == "NoPen" || damageType == "Miss"))
                    {
                        continue;
                    }

                    // In case of NoPen and MissileMiss in same action,
                    // only display MissileMisses, skip NoPens (and skip regular
                    // Misses too as done lines before)
                    if (damageInstance.Type == "NoPen" && (damageType == "MissileMiss"))
                    {
                        continue;
                    }

                    damageType = damageInstance.Type;
                    defenderDV = lastDefenderDV;
                    attackerRollAgainstDV = damageInstance.AttackerRollAgainstDV;
                    defenderAV = damageInstance.DefenderAV;
                    attackerRollAgainstAV = damageInstance.AttackerBestRollAgainstAV;
                    DefenderCell = damageInstance.DefenderCell;
                    AttackerCell = damageInstance.AttackerCell;
                    Defender = damageInstance.Defender;
                    Attacker = damageInstance.Attacker;
                    isAttackerThePlayer = damageInstance.IsAttackerThePlayer;
                }

                if (AttackerCell == null)
                {
                    Cleanup();
                    return;
                }

                float scale = 0.4f;
                int threshold;
                if (damageType == "Miss" || damageType == "MissileMiss")
                {
                    threshold = GetMissOrBlockThreshold(damageType, attackerRollAgainstDV, defenderDV);
                }
                else
                {
                    threshold = GetMissOrBlockThreshold(damageType, attackerRollAgainstAV, defenderAV);
                }
                UnityEngine.Color rollColor = PenetrationsColors.Colors[threshold];

                if (damageType == "Miss" || damageType == "MissileMiss")
                {
                    if (
                        (isAttackerThePlayer && ShowPlayerMissOrBlockRolls) || (!isAttackerThePlayer && ShowOtherMissOrBlockRolls)
                    )
                    {
                        CombatJuice.floatingText(
                            AttackerCell,
                            "[" + attackerRollAgainstDV + " vs " + defenderDV + "]",
                            rollColor,
                            1f,
                            18f,
                            scale,
                            true,
                            Attacker
                        );
                    }
                }
                else if (damageType == "NoPen")
                {
                    if (
                        (isAttackerThePlayer && ShowPlayerMissOrBlockRolls) || (!isAttackerThePlayer && ShowOtherMissOrBlockRolls)
                    )
                    {
                        CombatJuice.floatingText(
                            AttackerCell,
                            "[" + attackerRollAgainstAV + " vs " + defenderAV + "]",
                            rollColor,
                            1f,
                            18f,
                            scale,
                            true,
                            Attacker
                        );
                    }
                }
            }
            Cleanup();
        }

        static string GetDamageType(Damage damage)
        {
            string damageType;

            if (damage.HasAttribute("Vaporized"))
            {
                damageType = "Vaporized";
            }
            else if (damage.IsHeatDamage())
            {
                if (!damage.HasAttribute("Fire"))
                {
                    damageType = "Heat";
                }
                else
                {
                    damageType = "Fire";
                }
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
            else if (damage.HasAttribute("Explosion"))
            {
                damageType = "Explosion";
            }
            else if (damage.HasAttribute("Gas"))
            {
                damageType = "Gas";
            }
            else if (damage.HasAttribute("Astral"))
            {
                damageType = "Astral";
            }
            else if (damage.HasAttribute("Umbral"))
            {
                damageType = "Umbral";
            }
            else if (damage.HasAttribute("Vibro"))
            {
                damageType = "Vibro";
            }
            else
            {
                damageType = "Physical";
            }
            if (damage.HasAttribute("Cosmic"))
            {
                damageType = "Cosmic";
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

        static int GetMissOrBlockThreshold(string damageType, int attackerRoll, int defenderStat)
        {
            int threshold = 1;

            if (damageType == "Miss" || damageType == "NoPen" || damageType == "MissileMiss")
            {
                threshold = defenderStat - attackerRoll + 1;
            }

            if (threshold > 20)
            {
                threshold = 20;
            }
            else if (threshold > 10)
            {
                threshold = 10;
            }
            else if (threshold > 5)
            {
                threshold = 5;
            }
            else if (threshold <= 0)
            {
                threshold = 20;
            }

            return threshold;
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
