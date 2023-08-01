using XRL.World;

namespace Improved_Damage_Tooltips.Utilities
{
    public class CombatState
    {
        public bool isValidCombat;
        public bool isMultiCellDamage;
        public GameObject Defender;
        public Statistic defenderStat;
        public Cell attackerCell;
        public Cell defenderCell;
        public GameObject Attacker;
        public bool isAttackerThePlayer;
    }
}
