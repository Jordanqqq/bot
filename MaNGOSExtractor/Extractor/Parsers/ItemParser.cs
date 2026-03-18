using System.Collections.Generic;
using System.Linq;
using RaidLootCore.Models;
using MaNGOSExtractor.MPQ.Models;  

namespace MaNGOSExtractor.Extractor.Parsers
{
    public class ItemParser
    {
        private readonly Dictionary<int, Spell> _spellDatabase;

        public ItemParser(Dictionary<int, Spell> spellDatabase = null)
        {
            _spellDatabase = spellDatabase ?? new Dictionary<int, Spell>();
        }

        public string BuildTooltip(Item item)
        {
            var parts = new List<string>();

            // 1. Тип предмета
            if (!string.IsNullOrEmpty(item.Type))
            {
                parts.Add($"**{item.Type}**");
            }

            // 2. Урон и скорость для оружия
            if (item.MinDamage > 0 && item.MaxDamage > 0 && item.Speed > 0)
            {
                parts.Add($"**Урон:** {item.MinDamage:F1} - {item.MaxDamage:F1}  **Скорость:** {item.Speed:F2}");
                parts.Add($"**({item.DPS:F2} ед. урона в секунду)**");
            }

            // 3. Броня
            if (item.Armor > 0)
            {
                parts.Add($"**{item.Armor} брони**");
            }

            // 4. Основные характеристики (stat_type 1-5)
            if (item.Strength > 0) parts.Add($"+{item.Strength} к силе");
            if (item.Agility > 0) parts.Add($"+{item.Agility} к ловкости");
            if (item.Stamina > 0) parts.Add($"+{item.Stamina} к выносливости");
            if (item.Intellect > 0) parts.Add($"+{item.Intellect} к интеллекту");
            if (item.Spirit > 0) parts.Add($"+{item.Spirit} к духу");

            // 5. Рейтинговые характеристики (stat_type 6-14)
            if (item.AttackPower > 0) parts.Add($"+{item.AttackPower} к силе атаки");
            if (item.SpellPower > 0) parts.Add($"+{item.SpellPower} к силе заклинаний");
            if (item.CritRating > 0) parts.Add($"+{item.CritRating} к рейтингу критического удара");
            if (item.HasteRating > 0) parts.Add($"+{item.HasteRating} к рейтингу скорости");
            if (item.HitRating > 0) parts.Add($"+{item.HitRating} к рейтингу меткости");
            if (item.ExpertiseRating > 0) parts.Add($"+{item.ExpertiseRating} к рейтингу мастерства");
            if (item.ArmorPenetration > 0) parts.Add($"+{item.ArmorPenetration} к рейтингу пробивания брони");

            // 6. Сопротивления
            if (item.FireResist > 0) parts.Add($"+{item.FireResist} к сопротивлению огню");
            if (item.FrostResist > 0) parts.Add($"+{item.FrostResist} к сопротивлению льду");
            if (item.ShadowResist > 0) parts.Add($"+{item.ShadowResist} к сопротивлению тьме");
            if (item.NatureResist > 0) parts.Add($"+{item.NatureResist} к сопротивлению природе");
            if (item.ArcaneResist > 0) parts.Add($"+{item.ArcaneResist} к сопротивлению тайной магии");

            // 7. Сокеты
            if (item.Sockets?.Count > 0)
            {
                var socketIcons = new List<string>();
                foreach (var socket in item.Sockets)
                {
                    socketIcons.Add(socket.Color switch
                    {
                        "Red" => "🔴",
                        "Yellow" => "🟡",
                        "Blue" => "🔵",
                        "Meta" => "💎",
                        _ => "⚪"
                    });
                }
                parts.Add(string.Join(" ", socketIcons));

                if (!string.IsNullOrEmpty(item.SocketBonus))
                {
                    parts.Add($"**Бонус за гнезда:** {item.SocketBonus}");
                }
            }

            // 8. Прочность
            if (item.Durability > 0)
            {
                parts.Add($"**Прочность:** {item.Durability} / {item.Durability}");
            }

            // 9. Требования
            if (item.RequiredLevel > 0)
            {
                parts.Add($"**Требуется уровень {item.RequiredLevel}**");
            }

            return string.Join("\n", parts);
        }
    }
}