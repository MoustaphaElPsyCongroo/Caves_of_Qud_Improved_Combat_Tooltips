using XRL.World;

namespace Improved_Damage_Tooltips.Utilities
{
    public class DamageInstance
    {
        public int Amount { get; set; }
        public GameObject Defender { get; set; }
        public Cell Cell { get; set; }
        public Statistic Stat { get; set; }
        public string Type { get; set; }
        public UnityEngine.Color Color { get; set; }

        public DamageInstance(
            int amount,
            GameObject defender,
            string type,
            Statistic stat,
            Cell cell
        )
        {
            Amount = amount;
            Defender = defender;
            Cell = cell;
            Type = type;
            Color = DamageColors.Colors[type];
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
