using System.Collections.Generic;

namespace RaidLootCore.Models
{
    public class Item
    {
        public int Id { get; set; }
        public string NameRu { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string Quality { get; set; } = "";        // string, потому что в JSON так хранится
        public string Icon { get; set; } = "";
        public string TooltipRu { get; set; } = "";
        public string TooltipEn { get; set; } = "";
        public string RaidSize { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string BossName { get; set; } = "";
        public string Type { get; set; } = "";
        public int ItemLevel { get; set; }
        public int RequiredLevel { get; set; }
        public long BuyPrice { get; set; }
        public long SellPrice { get; set; }
        public string Flags { get; set; } = "";
        public string Bonding { get; set; } = "";
        public string Material { get; set; } = "";
        public string Sheath { get; set; } = "";

        // Характеристики
        public int Strength { get; set; }
        public int Agility { get; set; }
        public int Stamina { get; set; }
        public int Intellect { get; set; }
        public int Spirit { get; set; }

        // Боевые характеристики
        public int AttackPower { get; set; }
        public int SpellPower { get; set; }
        public int CritRating { get; set; }
        public int HasteRating { get; set; }
        public int HitRating { get; set; }
        public int ExpertiseRating { get; set; }
        public int ArmorPenetration { get; set; }

        // Для оружия
        public float MinDamage { get; set; }
        public float MaxDamage { get; set; }
        public float Speed { get; set; }
        public float DPS { get; set; }

        // Броня
        public int Armor { get; set; }
        public int Block { get; set; }

        // Сопротивления
        public int FireResist { get; set; }
        public int FrostResist { get; set; }
        public int ShadowResist { get; set; }
        public int NatureResist { get; set; }
        public int ArcaneResist { get; set; }

        // Сокеты
        public List<ItemSocket> Sockets { get; set; } = new();
        public string SocketBonus { get; set; } = "";

        // Прочее
        public int Durability { get; set; }
        public int AllowableClass { get; set; }
        public string Description { get; set; } = "";
    }

    public class ItemSocket
    {
        public string Color { get; set; } = "";
        public string Content { get; set; } = "";
    }
}