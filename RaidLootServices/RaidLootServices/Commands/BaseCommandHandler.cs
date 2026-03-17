using Discord;
using RaidLootCore.Models;
using System.Net;
using System.Text.RegularExpressions;

namespace RaidLootServices
{
    public abstract class BaseCommandHandler
    {
        protected readonly List<Item> _database;
        protected readonly RaidDataLoader _raidLoader;
        protected readonly RaidSessionManager _sessionManager;
        protected bool _useRussian = true;

        protected BaseCommandHandler(List<Item> database, RaidDataLoader raidLoader, RaidSessionManager sessionManager)
        {
            _database = database;
            _raidLoader = raidLoader;
            _sessionManager = sessionManager;
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        protected string GetBossNameLocalized(string? bossName, bool useRussian)
        {
            if (string.IsNullOrEmpty(bossName)) return "Неизвестно";

            var boss = _raidLoader.GetBossByName(bossName);
            if (boss != null && useRussian && !string.IsNullOrEmpty(boss.NameRu))
                return boss.NameRu;

            return bossName;
        }

        protected string GetDifficultyLocalized(string? difficulty, bool useRussian)
        {
            if (string.IsNullOrEmpty(difficulty)) return "";

            if (!useRussian) return difficulty;

            return difficulty.ToLower() switch
            {
                "normal" => "обычный",
                "heroic" => "героический",
                "mythic" => "мифический",
                _ => difficulty
            };
        }

        protected string GetQualityNameLocalized(string? quality, bool useRussian)
        {
            if (string.IsNullOrEmpty(quality) || !useRussian)
                return quality ?? "Неизвестно";

            string q = quality.ToLower();

            return q switch
            {
                "1" or "poor" => "Плохое",
                "2" or "common" => "Обычное",
                "3" or "uncommon" => "Необычное",
                "4" or "rare" => "Редкое",
                "5" or "epic" => "Эпическое",
                "6" or "legendary" => "Легендарное",
                _ => quality
            };
        }

        protected string GetTypeEmoji(string? itemType)
        {
            if (string.IsNullOrEmpty(itemType)) return "📦";

            return itemType.ToLower() switch
            {
                "меч" or "sword" => "⚔️",
                "кинжал" or "dagger" => "🗡️",
                "топор" or "axe" => "🪓",
                "молот" or "hammer" or "mace" => "🔨",
                "посох" or "staff" => "🪄",
                "лук" or "bow" => "🏹",
                "арбалет" or "crossbow" => "🎯",
                "щит" or "shield" => "🛡️",
                "шлем" or "helmet" => "⛑️",
                "наплечники" or "shoulders" => "🎭",
                "нагрудник" or "chest" => "👕",
                "перчатки" or "gloves" => "🧤",
                "пояс" or "belt" => "🔗",
                "штаны" or "pants" or "legs" => "👖",
                "сапоги" or "boots" => "👢",
                "кольцо" or "ring" => "💍",
                "амулет" or "necklace" or "neck" => "📿",
                "плащ" or "cloak" or "cape" => "🧥",
                "знак" or "sigil" => "🏅",
                _ => "📦"
            };
        }

        protected Color GetQualityColor(string? quality)
        {
            if (string.IsNullOrEmpty(quality)) return Color.LightGrey;

            string q = quality.Trim().ToLower();

            return q switch
            {
                "1" or "poor" => Color.DarkGrey,
                "2" or "common" => Color.LightGrey,
                "3" or "uncommon" => Color.Green,
                "4" or "rare" => new Color(0, 112, 221),
                "5" or "epic" => new Color(163, 53, 238),
                "6" or "legendary" => new Color(255, 128, 0),
                _ => Color.LightGrey
            };
        }

        protected string GetQualityEmoji(string? quality)
        {
            if (string.IsNullOrEmpty(quality)) return "⚪";

            string q = quality.Trim().ToLower();

            return q switch
            {
                "1" or "poor" => "⚫",
                "2" or "common" => "⚪",
                "3" or "uncommon" => "🟢",
                "4" or "rare" => "🔵",
                "5" or "epic" => "🟣",
                "6" or "legendary" => "🟠",
                _ => "⚪"
            };
        }

        protected string FormatTooltip(string input, string itemName)
        {
            if (string.IsNullOrEmpty(input)) return "";

            string decoded = WebUtility.HtmlDecode(input);
            string withBreaks = Regex.Replace(decoded, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
            string clean = Regex.Replace(withBreaks, "<.*?>", "").Trim();

            var lines = clean.Split('\n');
            var filteredLines = new List<string>();
            var gems = new List<string>();
            string socketBonus = "";

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                if (trimmed.Contains(itemName, StringComparison.OrdinalIgnoreCase)) continue;

                if (trimmed.Contains("Прочность:") ||
                    trimmed.Contains("Цена продажи:") ||
                    trimmed.Contains("Sell Price:") ||
                    trimmed.Contains("Durability:"))
                    continue;

                if (trimmed.Contains("гнездо") || trimmed.Contains("Socket"))
                {
                    string gemIcon = GetGemEmoji(trimmed);
                    gems.Add(gemIcon);
                    continue;
                }

                if (trimmed.Contains("При соответствии цвета:") ||
                    trimmed.Contains("Socket Bonus:"))
                {
                    socketBonus = trimmed.Replace("При соответствии цвета:", "")
                                        .Replace("Socket Bonus:", "")
                                        .Trim();
                    continue;
                }

                if (trimmed.Contains("Если на персонаже:") ||
                    trimmed.Contains("Equip:"))
                {
                    string effect = trimmed.Replace("Если на персонаже:", "")
                                          .Replace("Equip:", "")
                                          .Trim();
                    filteredLines.Add($"✨ {effect}");
                    continue;
                }

                if (trimmed.Contains("Использование:") ||
                    trimmed.Contains("Use:"))
                {
                    string use = trimmed.Replace("Использование:", "")
                                       .Replace("Use:", "")
                                       .Trim();
                    filteredLines.Add($"⚡ {use}");
                    continue;
                }

                filteredLines.Add(trimmed);
            }

            if (gems.Count > 0)
            {
                string gemLine = string.Join(" ", gems) +
                                 (string.IsNullOrEmpty(socketBonus) ? "" : $" 🎁 {socketBonus}");
                filteredLines.Insert(0, gemLine);
            }

            return string.Join("\n", filteredLines);
        }

        protected string GetGemEmoji(string text)
        {
            if (text.Contains("Красное") || text.Contains("Red")) return "🔴";
            if (text.Contains("Желтое") || text.Contains("Yellow")) return "🟡";
            if (text.Contains("Синее") || text.Contains("Blue")) return "🔵";
            if (text.Contains("Зеленое") || text.Contains("Green")) return "🟢";
            if (text.Contains("Фиолетовое") || text.Contains("Purple")) return "🟣";
            if (text.Contains("Оранжевое") || text.Contains("Orange")) return "🟠";
            if (text.Contains("Мета") || text.Contains("Meta")) return "💎";
            if (text.Contains("Бесцветное") || text.Contains("Prismatic")) return "⚪";
            return "🔘";
        }
    }
}