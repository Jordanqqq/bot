using Discord;
using Discord.WebSocket;
using RaidLootCore.Models;

namespace RaidLootServices
{
    public class RaidCommandHandler : BaseCommandHandler
    {
        public RaidCommandHandler(List<Item> database, RaidDataLoader raidLoader, RaidSessionManager sessionManager)
            : base(database, raidLoader, sessionManager) { }

        public async Task HandleRaidCommand(SocketSlashCommand command)
        {
            try
            {
                await command.DeferAsync(ephemeral: true);

                // Сначала показываем выбор сложности
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

        public async Task HandleDifficultySelected(SocketMessageComponent component, string difficulty)
        {
            try
            {
                // Сохраняем выбранную сложность в сессии
                var session = _sessionManager.GetSession(component.User.Id);
                if (session == null)
                {
                    session = _sessionManager.CreateSession(component.User.Id, "Не выбран", "Не выбрана");
                }

                string difficultyDisplay = difficulty switch
                {
                    "10n" => "10 Normal",
                    "10h" => "10 Heroic",
                    "25n" => "25 Normal",
                    "25h" => "25 Heroic",
                    _ => difficulty
                };

                session.Difficulty = difficultyDisplay;

                // Получаем список рейдов
                var raids = _raidLoader.GetAvailableRaids();

                if (raids.Count == 0)
                {
                    await component.FollowupAsync("❌ Нет доступных рейдов в базе", ephemeral: true);
                    return;
                }

                var raidMenu = new SelectMenuBuilder()
                    .WithPlaceholder("🏰 Выберите рейд")
                    .WithCustomId("select_raid")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                var raidEmojis = new Dictionary<string, string>
                {
                    ["Наксрамас"] = "🕷️",
                    ["Цитадель Ледяной Короны"] = "❄️🏰",
                    ["Ульдуар"] = "⚙️🗿",
                    ["Испытание крестоносца"] = "🏆",
                    ["Логово Ониксии"] = "🐉",
                    ["Око Вечности"] = "👁️✨",
                    ["Рубиновое Святилище"] = "🔴🏰",
                    ["Склеп Аркавона"] = "🗿"
                };

                foreach (var raid in raids.Take(25))
                {
                    string emoji = raidEmojis.ContainsKey(raid) ? raidEmojis[raid] : "🏰";
                    string value = raid.Replace(" ", "_").Replace("'", "").Replace("-", "_");
                    raidMenu.AddOption($"{emoji} {raid}", value, $"Боссы рейда {raid}");
                }

                var builder = new ComponentBuilder().WithSelectMenu(raidMenu);

                await component.UpdateAsync(msg => {
                    msg.Content = $"⚔️ **Сложность: {difficultyDisplay}**\n🏰 **Теперь выберите рейд:**";
                    msg.Components = builder.Build();
                    msg.Embed = null;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА DIFFICULTY] {ex.Message}");
                await component.FollowupAsync($"❌ Ошибка: {ex.Message}", ephemeral: true);
            }
        }

        public async Task HandleSelectRaid(SocketMessageComponent component)
        {
            try
            {
                string value = component.Data.Values.First();
                string raidName = value.Replace("_", " ");

                var session = _sessionManager.GetSession(component.User.Id);
                if (session == null)
                {
                    await component.FollowupAsync("❌ Сессия не найдена. Начните заново с /raid", ephemeral: true);
                    return;
                }

                session.RaidName = raidName;

                var bosses = _raidLoader.GetBossesForRaid(raidName);

                if (bosses.Count == 0)
                {
                    await component.FollowupAsync($"❌ Для рейда {raidName} нет боссов", ephemeral: true);
                    return;
                }

                var bossSelectMenu = new SelectMenuBuilder()
                    .WithPlaceholder($"👑 Выберите босса в {raidName}")
                    .WithCustomId("select_boss")
                    .WithMinValues(1)
                    .WithMaxValues(1);

                foreach (var boss in bosses.Take(25))
                {
                    string bossName = _useRussian && !string.IsNullOrEmpty(boss.NameRu)
                        ? boss.NameRu
                        : boss.NameEn ?? boss.Name;

                    string bossKey = $"{raidName}_{boss.NameEn ?? boss.Name}".Replace(" ", "_");
                    string emoji = !string.IsNullOrEmpty(boss.Emoji) ? boss.Emoji : "👑";

                    bossSelectMenu.AddOption($"{emoji} {bossName}", bossKey, $"Лут с босса {bossName}");
                }

                var builder = new ComponentBuilder().WithSelectMenu(bossSelectMenu);

                await component.UpdateAsync(msg => {
                    msg.Content = $"⚔️ **{raidName} ({session.Difficulty})** — выберите босса:";
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
    }
}