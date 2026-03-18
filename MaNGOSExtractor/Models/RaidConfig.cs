namespace RaidLootCore.Models;

public class RaidConfig
{
    public string RaidName { get; set; }
    public string RaidNameEn { get; set; }
    public List<BossConfig> Bosses { get; set; } = new();
}

public class BossConfig
{
    public string NameRu { get; set; }
    public string NameEn { get; set; }
    public string Emoji { get; set; }
    public List<int> ItemIds { get; set; } = new();
}