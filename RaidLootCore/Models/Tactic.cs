using System.Collections.Generic;
using RaidLootCore.Models;
namespace MaNGOSExtractor.Models
{
    public class BossTactic
    {
        public int BossId { get; set; }
        public string BossName { get; set; } = "";
        public List<SpellTactic> Spells { get; set; } = new();
        public List<PhaseTactic> Phases { get; set; } = new();
    }

    public class SpellTactic
    {
        public int SpellId { get; set; }
        public string SpellName { get; set; } = "";
        public string WarningMessage { get; set; } = "";     // Что говорить игроку
        public string AdviceMessage { get; set; } = "";      // Что делать игроку
        public string Role { get; set; } = "";                // tank/healer/dps
        public int Priority { get; set; } = 1;                // Приоритет предупреждения
        public float PreWarningTime { get; set; } = 5;        // За сколько секунд предупреждать
        public List<string> Classes { get; set; } = new();    // Для каких классов важно
    }

    public class PhaseTactic
    {
        public int PhaseNumber { get; set; }
        public string PhaseName { get; set; } = "";
        public string WarningMessage { get; set; } = "";
        public List<int> SpellIds { get; set; } = new();
    }
}