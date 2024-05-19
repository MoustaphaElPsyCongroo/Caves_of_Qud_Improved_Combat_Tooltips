using XRL.World;

namespace Gokudera_ElPsyCongroo_ICTooltips.Utilities
{
    public class CombatState
    {
        public bool isValidCombat;
        public bool isMultiCellDamage;
        public GameObject Defender;
        public Statistic DefenderStat;
        public Cell AttackerCell;
        public Cell DefenderCell;
        public GameObject Attacker;
        public bool isAttackerThePlayer;
    }
}
