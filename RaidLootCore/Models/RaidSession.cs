using System.Collections.Generic;

namespace RaidLootCore.Models
{
    public class RaidSession
    {
        public ulong LeaderId { get; set; }
        public string RaidName { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string CurrentBoss { get; set; } = "";
        public Dictionary<int, ulong> Reserves { get; set; } = new Dictionary<int, ulong>();
    }
}