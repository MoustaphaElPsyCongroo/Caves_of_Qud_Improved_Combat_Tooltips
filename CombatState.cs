using XRL.World;

namespace Gokudera_ElPsyCongroo_ICTooltips.Utilities
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
