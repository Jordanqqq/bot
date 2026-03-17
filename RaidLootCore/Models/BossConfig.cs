using System.Collections.Generic;

namespace RaidLootCore.Models
{
    public class BossConfig
    {
        public string Name { get; set; } = "";
        public string? NameRu { get; set; }
        public string? NameEn { get; set; }
        public string? Emoji { get; set; }
        public List<int>? ItemIds { get; set; }
    }
}