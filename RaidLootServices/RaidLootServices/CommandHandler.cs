using Discord;
using Discord.WebSocket;
using RaidLootCore.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace RaidLootServices
{
    public class CommandHandler
    {
        private readonly List<Item> _database;
        private readonly RaidDataLoader _raidLoader;
        private readonly RaidSessionManager _sessionManager;
        private bool _useRussian = true;
        private List<LootData> _lootCache = new(); // Кэш для лута

        public CommandHandler(List<Item> database, RaidDataLoader raidLoader, RaidSessionManager sessionManager)
        {
            _database = database;
            _raidLoader = raidLoader;
            _sessionManager = sessionManager;

            // Загружаем loot.json при инициализации
            LoadLootData();

            Console.WriteLine($"[БОТ] Загружено предметов: {database.Count}");
        }

        private void LoadLootData()
        {
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
                string lootPath = Path.Combine(projectRoot, "Output", "loot.json");

                if (File.Exists(lootPath))
                {
                    var lootJson = File.ReadAllText(lootPath);
                    _lootCache = JsonSerializer.Deserialize<List<LootData>>(lootJson) ?? new();
                    Console.WriteLine($"[БОТ] Загружено {_lootCache.Count} записей лута");
                }
                else
                {
                    Console.WriteLine($"[БОТ] ⚠️ Файл loot.json не найден: {lootPath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[БОТ] ❌ Ошибка загрузки loot.json: {ex.Message}");
            }
        }

        // ========== ОБРАБОТКА СЛЭШ-КОМАНД ==========

        public async Task HandleRaidCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);

                var difficultyMenu = new SelectMenuBuilder()
                    .WithPlaceholder("⚔️ Выберите сложность")
                    .WithCustomId("select_difficulty")
                    .WithMinValues(1)
                    .WithMaxValues(1)
                    .AddOption("10 Normal", "10n", "Обычный режим на 10 человек")
                    .AddOption("10 Heroic", "10h", "Героический режим на 10 человек")
                    .AddOption("25 Normal", "25n", "Обычный режим на 25 человек")
                    .AddOption("25 Heroic", "25h", "Героический режим на 25 человек");

                var builder = new ComponentBuilder().WithSelectMenu(difficultyMenu);
                await command.FollowupAsync("⚔️ **Сначала выберите сложность рейда:**",
                    components: builder.Build(), ephemeral: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА RAID] {ex.Message}");
                await command.FollowupAsync($"❌ Ошибка: {ex.Message}", ephemeral: true);
            }
        }

        public async Task HandleLootCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);

                var query = command.Data.Options.First().Value.ToString()?.Trim().ToLower();

                var item = _database.FirstOrDefault(x =>
                    (x.NameRu != null && x.NameRu.ToLower().Contains(query)) ||
                    (x.NameEn != null && x.NameEn.ToLower().Contains(query)));

                if (item == null)
                {
                    await command.FollowupAsync("❌ Предмет не найден.", ephemeral: true);
                    return;
                }

                bool useRussian = _useRussian || (item.NameRu != null && item.NameRu.ToLower().Contains(query));

                string itemName = useRussian ? item.NameRu : item.NameEn;
                string tooltip = useRussian ? item.TooltipRu : item.TooltipEn;

                var embed = new EmbedBuilder()
                    .WithTitle($"{GetQualityEmoji(item.Quality)} {itemName}")
                    .WithDescription(tooltip ?? "Нет описания")
                    .WithThumbnailUrl($"https://wow.zamimg.com/images/wow/icons/large/{item.Icon}.jpg")
                    .WithColor(GetQualityColor(item.Quality))
                    .WithFooter(footer => footer.Text = "Raid Loot System • 2026")
                    .WithCurrentTimestamp()
                    .Build();

                await command.FollowupAsync(embed: embed, ephemeral: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА LOOT] {ex.Message}");
                await command.FollowupAsync($"❌ Ошибка: {ex.Message}", ephemeral: true);
            }
        }

        public async Task HandleCreateRaidCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);

                var raidName = command.Data.Options.First().Value.ToString();
                var difficulty = command.Data.Options.Last().Value.ToString();

                var session = _sessionManager.CreateSession(command.User.Id, raidName!, difficulty!);

                var embed = new EmbedBuilder()
                    .WithTitle("✅ Рейд создан!")
                    .WithDescription($"**{raidName}** ({difficulty})")
                    .AddField("Рейд-лидер", command.User.Mention, true)
                    .AddField("Команды", "`/add_reserve` - добавить резерв\n`/show_reserves` - показать резервы")
                    .WithColor(Color.Green)
                    .WithCurrentTimestamp()
                    .Build();

                await command.FollowupAsync(embed: embed);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА CREATE RAID] {ex.Message}");
                await command.FollowupAsync($"❌ Ошибка: {ex.Message}", ephemeral: true);
            }
        }

        public async Task HandleAddReserveCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);

                var session = _sessionManager.GetSession(command.User.Id);
                if (session == null)
                {
                    await command.FollowupAsync("❌ У вас нет активного рейда", ephemeral: true);
                    return;
                }

                var itemName = command.Data.Options.First().Value.ToString();

                var item = _database.FirstOrDefault(x =>
                    (x.NameRu != null && x.NameRu.Contains(itemName!, StringComparison.OrdinalIgnoreCase)) ||
                    (x.NameEn != null && x.NameEn.Contains(itemName!, StringComparison.OrdinalIgnoreCase)));

                if (item == null)
                {
                    await command.FollowupAsync("❌ Предмет не найден", ephemeral: true);
                    return;
                }

                if (_sessionManager.TryReserve(command.User.Id, item.Id, command.User.Id))
                {
                    string displayName = _useRussian && !string.IsNullOrEmpty(item.NameRu) ? item.NameRu : item.NameEn;
                    await command.FollowupAsync($"✅ Предмет **{displayName}** зарезервирован!", ephemeral: true);
                }
                else
                {
                    await command.FollowupAsync("❌ Предмет уже зарезервирован", ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА ADD RESERVE] {ex.Message}");
                await command.FollowupAsync($"❌ Ошибка: {ex.Message}", ephemeral: true);
            }
        }

        public async Task HandleShowReservesCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);

                var session = _sessionManager.GetSession(command.User.Id);
                if (session == null)
                {
                    await command.FollowupAsync("❌ У вас нет активного рейда", ephemeral: true);
                    return;
                }

                var reserves = _sessionManager.GetReserves(command.User.Id);

                if (reserves.Count == 0)
                {
                    await command.FollowupAsync("📭 У вас нет активных резервов", ephemeral: true);
                    return;
                }

                var description = "";
                foreach (var reserve in reserves)
                {
                    var item = _database.FirstOrDefault(x => x.Id == reserve.Key);
                    if (item != null)
                    {
                        string itemName = _useRussian && !string.IsNullOrEmpty(item.NameRu) ? item.NameRu : item.NameEn;
                        description += $"• {itemName} — <@{reserve.Value}>\n";
                    }
                }

                var embed = new EmbedBuilder()
                    .WithTitle($"📋 Резервы рейда {session.RaidName}")
                    .WithDescription(description)
                    .WithColor(Color.Blue)
                    .WithCurrentTimestamp()
                    .Build();

                await command.FollowupAsync(embed: embed, ephemeral: true);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА SHOW RESERVES] {ex.Message}");
                await command.FollowupAsync($"❌ Ошибка: {ex.Message}", ephemeral: true);
            }
        }

        // ========== МЕТОДЫ ДЛЯ DISCORDBOTSERVICE ==========

        public async Task HandleSelectRaid(SocketMessageComponent component)
        {
            try
            {
                string value = component.Data.Values.First();
                string raidName = value.Replace("_", " ");

                var bosses = _raidLoader?.GetBossesForRaid(raidName) ?? new List<BossConfig>();

                if (bosses.Count == 0)
                {
                    await component.FollowupAsync($"❌ Для рейда {raidName} нет боссов", ephemeral: true);
                    return;
                }

                var bossMenu = new SelectMenuBuilder()
                    .WithPlaceholder($"👑 Выберите босса в {raidName}")
                    .WithCustomId("select_boss")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                foreach (var boss in bosses.Take(25))
                {
                    string bossName = _useRussian && !string.IsNullOrEmpty(boss.NameRu)
                        ? boss.NameRu
                        : boss.NameEn ?? boss.Name;

                    string bossKey = (boss.NameEn ?? boss.Name).Replace(" ", "_");
                    string emoji = !string.IsNullOrEmpty(boss.Emoji) ? boss.Emoji : "👑";

                    bossMenu.AddOption($"{emoji} {bossName}", bossKey, $"Лут с босса {bossName}");
                }

                var builder = new ComponentBuilder().WithSelectMenu(bossMenu);

                await component.UpdateAsync(msg => {
                    msg.Content = $"🏰 **{raidName}** — выберите босса:";
                    msg.Components = builder.Build();
                    msg.Embed = null;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА SELECT RAID] {ex.Message}");
                await component.FollowupAsync($"❌ Ошибка: {ex.Message}", ephemeral: true);
            }
        }

        // ========== ОБРАБОТКА МЕНЮ ==========

        public async Task HandleMenuSelection(SocketMessageComponent component)
        {
            try
            {
                string customId = component.Data.CustomId;
                Console.WriteLine($"[МЕНЮ] {customId} от {component.User.Username}");

                switch (customId)
                {
                    case "select_difficulty":
                        await HandleDifficultySelected(component);
                        break;
                    case "select_raid":
                        await HandleRaidSelected(component);
                        break;
                    case "select_boss":
                        await HandleBossSelected(component);
                        break;
                    case "select_item":
                        await HandleItemSelected(component);
                        break;
                    case "toggle_language":
                        _useRussian = !_useRussian;
                        await component.UpdateAsync(msg => {
                            msg.Content = $"✅ Язык переключен на {(_useRussian ? "русский" : "английский")}";
                            msg.Components = null;
                            msg.Embed = null;
                        });
                        break;
                    default:
                        if (customId.StartsWith("reserve_"))
                        {
                            if (int.TryParse(customId.Replace("reserve_", ""), out int itemId))
                            {
                                await HandleReserve(component, itemId);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА МЕНЮ] {ex.Message}");
                await component.RespondAsync($"❌ Ошибка: {ex.Message}", ephemeral: true);
            }
        }

        private async Task HandleDifficultySelected(SocketMessageComponent component)
        {
            string difficulty = component.Data.Values.First();
            string difficultyDisplay = difficulty switch
            {
                "10n" => "10 Normal",
                "10h" => "10 Heroic",
                "25n" => "25 Normal",
                "25h" => "25 Heroic",
                _ => difficulty
            };

            var session = _sessionManager.GetSession(component.User.Id);
            if (session == null)
            {
                session = _sessionManager.CreateSession(component.User.Id, "Не выбран", difficultyDisplay);
            }
            else
            {
                session.Difficulty = difficultyDisplay;
            }

            var raids = _raidLoader?.GetAvailableRaids() ?? new List<string>();

            if (raids.Count == 0)
            {
                await component.FollowupAsync("❌ Нет доступных рейдов", ephemeral: true);
                return;
            }

            var raidMenu = new SelectMenuBuilder()
                .WithPlaceholder("🏰 Выберите рейд")
                .WithCustomId("select_raid")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var raid in raids.Take(25))
            {
                raidMenu.AddOption(raid, raid.Replace(" ", "_"), $"Боссы рейда {raid}");
            }

            var builder = new ComponentBuilder().WithSelectMenu(raidMenu);

            await component.UpdateAsync(msg => {
                msg.Content = $"⚔️ **Сложность: {difficultyDisplay}**\n🏰 **Теперь выберите рейд:**";
                msg.Components = builder.Build();
                msg.Embed = null;
            });
        }

        private async Task HandleRaidSelected(SocketMessageComponent component)
        {
            string value = component.Data.Values.First();
            string raidName = value.Replace("_", " ");

            var bosses = _raidLoader?.GetBossesForRaid(raidName) ?? new List<BossConfig>();

            if (bosses.Count == 0)
            {
                await component.FollowupAsync($"❌ Для рейда {raidName} нет боссов", ephemeral: true);
                return;
            }

            var bossMenu = new SelectMenuBuilder()
                .WithPlaceholder($"👑 Выберите босса в {raidName}")
                .WithCustomId("select_boss")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var boss in bosses.Take(25))
            {
                string bossName = _useRussian && !string.IsNullOrEmpty(boss.NameRu)
                    ? boss.NameRu
                    : boss.NameEn ?? boss.Name;

                string bossKey = (boss.NameEn ?? boss.Name).Replace(" ", "_");
                string emoji = !string.IsNullOrEmpty(boss.Emoji) ? boss.Emoji : "👑";

                bossMenu.AddOption($"{emoji} {bossName}", bossKey, $"Лут с босса {bossName}");
            }

            var builder = new ComponentBuilder().WithSelectMenu(bossMenu);

            await component.UpdateAsync(msg => {
                msg.Content = $"🏰 **{raidName}** — выберите босса:";
                msg.Components = builder.Build();
                msg.Embed = null;
            });
        }

        private async Task HandleBossSelected(SocketMessageComponent component)
        {
            string bossKey = component.Data.Values.First();
            string bossName = bossKey.Replace("_", " ");

            // Ищем босса в кэше лута
            var bossLoot = _lootCache?.FirstOrDefault(l =>
                l.BossName.Contains(bossName, StringComparison.OrdinalIgnoreCase));

            if (bossLoot == null || bossLoot.Items == null || bossLoot.Items.Count == 0)
            {
                await component.UpdateAsync(msg => {
                    msg.Content = $"❌ Нет предметов для босса {bossName}";
                    msg.Components = null;
                });
                return;
            }

            var itemMenu = new SelectMenuBuilder()
                .WithPlaceholder("📦 Выберите предмет")
                .WithCustomId("select_item")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var lootItem in bossLoot.Items.Take(25))
            {
                var item = _database.FirstOrDefault(i => i.Id == lootItem.ItemId);
                if (item != null)
                {
                    string itemName = _useRussian ? (item.NameRu ?? item.NameEn) : (item.NameEn ?? "");
                    itemMenu.AddOption(
                        itemName.Length > 100 ? itemName.Substring(0, 97) + "..." : itemName,
                        item.Id.ToString(),
                        $"Уровень: {item.ItemLevel} | Шанс: {lootItem.DropChance:F1}%"
                    );
                }
            }

            var builder = new ComponentBuilder().WithSelectMenu(itemMenu);

            await component.UpdateAsync(msg => {
                msg.Content = $"👑 **{bossName}** — выберите предмет:";
                msg.Components = builder.Build();
                msg.Embed = null;
            });
        }

        private async Task HandleItemSelected(SocketMessageComponent component)
        {
            if (!int.TryParse(component.Data.Values.First(), out int itemId))
            {
                await component.RespondAsync("❌ Неверный ID предмета", ephemeral: true);
                return;
            }

            var item = _database.FirstOrDefault(x => x.Id == itemId);

            if (item == null)
            {
                await component.RespondAsync("❌ Предмет не найден", ephemeral: true);
                return;
            }

            string itemName = _useRussian ? (item.NameRu ?? item.NameEn) : (item.NameEn ?? "Неизвестно");
            string tooltip = _useRussian ? item.TooltipRu : item.TooltipEn;

            var embed = new EmbedBuilder()
                .WithTitle($"{GetQualityEmoji(item.Quality)} {itemName}")
                .WithDescription(tooltip ?? "Нет описания")
                .WithThumbnailUrl($"https://wow.zamimg.com/images/wow/icons/large/{item.Icon}.jpg")
                .WithColor(GetQualityColor(item.Quality))
                .WithFooter(footer => footer.Text = "Raid Loot System • 2026")
                .WithCurrentTimestamp()
                .Build();

            var button = new ComponentBuilder()
                .WithButton("✅ Зарезервировать", $"reserve_{item.Id}", ButtonStyle.Success)
                .WithButton("🇷🇺/🇬🇧", "toggle_language", ButtonStyle.Primary)
                .Build();

            await component.UpdateAsync(msg => {
                msg.Content = null;
                msg.Embed = embed;
                msg.Components = button;
            });
        }

        private async Task HandleReserve(SocketMessageComponent component, int itemId)
        {
            var session = _sessionManager.GetSession(component.User.Id);
            if (session == null)
            {
                await component.FollowupAsync("❌ У вас нет активного рейда", ephemeral: true);
                return;
            }

            var item = _database.FirstOrDefault(x => x.Id == itemId);
            if (item == null)
            {
                await component.FollowupAsync("❌ Предмет не найден", ephemeral: true);
                return;
            }

            if (_sessionManager.TryReserve(component.User.Id, itemId, component.User.Id))
            {
                string itemName = _useRussian && !string.IsNullOrEmpty(item.NameRu) ? item.NameRu : item.NameEn;
                await component.FollowupAsync($"✅ Предмет **{itemName}** зарезервирован!", ephemeral: true);
            }
            else
            {
                await component.FollowupAsync("❌ Предмет уже зарезервирован", ephemeral: true);
            }
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        private Color GetQualityColor(string? quality)
        {
            if (string.IsNullOrEmpty(quality)) return Color.LightGrey;
            return int.TryParse(quality, out int q) ? q switch
            {
                3 => new Color(0, 112, 221),
                4 => new Color(163, 53, 238),
                5 => new Color(255, 128, 0),
                _ => Color.LightGrey
            } : Color.LightGrey;
        }

        private string GetQualityEmoji(string? quality)
        {
            if (string.IsNullOrEmpty(quality)) return "⚪";
            return int.TryParse(quality, out int q) ? q switch
            {
                3 => "🔵",
                4 => "🟣",
                5 => "🟠",
                _ => "⚪"
            } : "⚪";
        }
    }

    // Классы для десериализации loot.json
    public class LootData
    {
        public int BossId { get; set; }
        public string BossName { get; set; } = "";
        public List<LootItemData> Items { get; set; } = new();
    }

    public class LootItemData
    {
        public int ItemId { get; set; }
        public string ItemName { get; set; } = "";
        public float DropChance { get; set; }
        public int MinCount { get; set; }
        public int MaxCount { get; set; }
        public bool IsQuestItem { get; set; }
    }
}