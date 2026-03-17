using System.Collections.Generic;

namespace RaidLootCore.Models
{
    public class RaidConfig
    {
        public string RaidName { get; set; } = "";
        public string? RaidNameEn { get; set; }
        public List<BossConfig> Bosses { get; set; } = new();
    }
}