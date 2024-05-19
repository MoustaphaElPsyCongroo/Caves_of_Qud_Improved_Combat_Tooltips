using XRL.World;

namespace Gokudera_ElPsyCongroo_ICTooltips.Utilities
{
    public class DamageInstance
    {
        public int Amount { get; set; }
        public int Penetrations { get; set; }
        public string Type { get; set; }
        public GameObject Defender { get; set; }
        public GameObject Attacker { get; set; }
        public int DefenderAV { get; set; }
        public int AttackerBestRollAgainstAV { get; set; }
        public int AttackerRollAgainstDV { get; set; }
        public bool IsAttackerThePlayer { get; set; }
        public bool IsDefenderTakingDamage { get; set; }
        public Statistic Stat { get; set; }
        public Cell DefenderCell { get; set; }
        public Cell AttackerCell { get; set; }
        public UnityEngine.Color Color { get; set; }

        public DamageInstance(
            int amount,
            int penetrations,
            string type,
            int defenderAV,
            int attackerBestRollAgainstAV,
            int attackerRollAgainstDV,
            GameObject defender,
            GameObject attacker,
            bool isAttackerThePlayer,
            bool isDefenderTakingDamage,
            Statistic stat,
            Cell defenderCell,
            Cell attackerCell
        )
        {
            Amount = amount;
            Penetrations = penetrations;
            Type = type;
            DefenderAV = defenderAV;
            AttackerBestRollAgainstAV = attackerBestRollAgainstAV;
            AttackerRollAgainstDV = attackerRollAgainstDV;
            Defender = defender;
            Attacker = attacker;
            IsAttackerThePlayer = isAttackerThePlayer;
            IsDefenderTakingDamage = isDefenderTakingDamage;
            Stat = stat;
            DefenderCell = defenderCell;
            AttackerCell = attackerCell;
            Color =
                type != "Miss" && type != "NoPen" && type != "MissileMiss"
                    ? DamageColors.Colors[type]
                    : DamageColors.Colors["Physical"];
        }

        public float GetScale()
        {
            float scale = 1f;
            if (Stat?.BaseValue > 0)
            {
                float num5 = (float)Amount / (float)Stat.BaseValue;
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
