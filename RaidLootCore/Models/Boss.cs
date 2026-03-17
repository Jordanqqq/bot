using System.Collections.Generic;

namespace RaidLootCore.Models
{
    public class Boss
    {
        public int Id { get; set; }
        public string NameRu { get; set; } = "";
        public string NameEn { get; set; } = "";
        public int Level { get; set; }
        public string Type { get; set; } = "";
        public string PortraitFile { get; set; } = "";
        public int DisplayId { get; set; }
        public string Zone { get; set; } = "";
        public string Raid { get; set; } = "";
        public float Health { get; set; }
        public int MinGold { get; set; }
        public int MaxGold { get; set; }
        public int MapId { get; set; }
        public List<LootItem> LootTable { get; set; } = new();
    }

    public class LootItem
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public float DropChance { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
        public bool IsQuestItem { get; set; }
    }
}