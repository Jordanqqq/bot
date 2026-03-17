using Discord;
using Discord.WebSocket;
using RaidLootCore.Models;

namespace RaidLootServices
{
    public class SessionCommandHandler : BaseCommandHandler
    {
        public SessionCommandHandler(List<Item> database, RaidDataLoader raidLoader, RaidSessionManager sessionManager)
            : base(database, raidLoader, sessionManager) { }

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

        public async Task HandleSelectBoss(SocketMessageComponent component, string bossKey)
        {
            string[] parts = bossKey.Split('_');
            string raidName = parts[0];
            string bossNameEn = string.Join("_", parts.Skip(1)).Replace("_", " ");

            var session = _sessionManager.GetSession(component.User.Id);
            if (session == null)
            {
                await component.FollowupAsync("❌ Сессия не найдена. Начните заново с /raid", ephemeral: true);
                return;
            }

            session.CurrentBoss = bossNameEn;
            session.RaidName = raidName;

            var boss = _raidLoader.GetBossByName(bossNameEn);
            string displayName = _useRussian && boss != null && !string.IsNullOrEmpty(boss.NameRu)
                ? boss.NameRu
                : bossNameEn;

            // Получаем предметы для выбранного босса и сложности
            var items = _database.Where(x =>
                x.BossName == bossNameEn &&
                $"{x.RaidSize} {x.Difficulty}" == session.Difficulty).ToList();

            if (items.Count == 0)
            {
                // Пробуем найти по английскому имени босса без пробелов
                items = _database.Where(x =>
                    x.BossName?.Replace(" ", "") == bossNameEn?.Replace(" ", "") &&
                    $"{x.RaidSize} {x.Difficulty}" == session.Difficulty).ToList();
            }

            if (items.Count == 0)
            {
                await component.UpdateAsync(msg => {
                    msg.Content = $"❌ Нет предметов для {displayName} ({session.Difficulty})";
                    msg.Components = null;
                    msg.Embed = null;
                });
                return;
            }

            // Показываем предметы
            var itemMenu = new SelectMenuBuilder()
                .WithPlaceholder($"📦 Предметы ({session.Difficulty})")
                .WithCustomId("select_item")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var item in items.Take(25))
            {
                string itemName = _useRussian && !string.IsNullOrEmpty(item.NameRu)
                    ? item.NameRu
                    : item.NameEn ?? "Без названия";

                string emoji = GetTypeEmoji(item.Type);
                string quality = GetQualityNameLocalized(item.Quality, _useRussian);

                itemMenu.AddOption($"{emoji} {itemName}", item.Id.ToString(),
                    $"{quality} уровень {item.ItemLevel}");
            }

            var builder = new ComponentBuilder().WithSelectMenu(itemMenu);

            await component.UpdateAsync(msg => {
                msg.Content = $"📦 **{displayName} ({session.Difficulty})** — выберите предмет:";
                msg.Components = builder.Build();
                msg.Embed = null;
            });
        }

        public async Task HandleDifficultyChoice(SocketMessageComponent component, string difficulty)
        {
            // Этот метод больше не нужен, так как сложность выбирается в начале
            await Task.CompletedTask;
        }
    }
}