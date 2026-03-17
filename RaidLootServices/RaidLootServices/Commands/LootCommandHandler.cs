using Discord;
using Discord.WebSocket;
using RaidLootCore.Models;

namespace RaidLootServices
{
    public class LootCommandHandler : BaseCommandHandler
    {
        public LootCommandHandler(List<Item> database, RaidDataLoader raidLoader, RaidSessionManager sessionManager)
            : base(database, raidLoader, sessionManager) { }

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

                bool useRussian = _useRussian ||
                    (item.NameRu != null && item.NameRu.ToLower().Contains(query));

                string itemName = useRussian ? item.NameRu : item.NameEn;
                string tooltip = useRussian ? item.TooltipRu : item.TooltipEn;
                string bossName = GetBossNameLocalized(item.BossName, useRussian);
                string size = item.RaidSize ?? "";
                string diff = GetDifficultyLocalized(item.Difficulty, useRussian);

                var embed = new EmbedBuilder()
                    .WithTitle($"{GetQualityEmoji(item.Quality)} {itemName}")
                    .WithDescription($"**{(useRussian ? "Босс" : "Boss")}:** {bossName}\n" +
                                     $"**{(useRussian ? "Сложность" : "Difficulty")}:** {size} {diff}\n\n" +
                                     $"{FormatTooltip(tooltip, itemName)}")
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

        public async Task HandleSelectItem(SocketMessageComponent component)
        {
            try
            {
                int itemId = int.Parse(component.Data.Values.First());
                var item = _database.FirstOrDefault(x => x.Id == itemId);

                if (item == null)
                {
                    await component.FollowupAsync("❌ Предмет не найден", ephemeral: true);
                    return;
                }

                string itemName = _useRussian && !string.IsNullOrEmpty(item.NameRu)
                    ? item.NameRu
                    : item.NameEn ?? "Неизвестно";

                string tooltip = _useRussian && !string.IsNullOrEmpty(item.TooltipRu)
                    ? item.TooltipRu
                    : item.TooltipEn ?? "";

                string bossName = _useRussian
                    ? _raidLoader.GetBossByName(item.BossName)?.NameRu ?? item.BossName
                    : item.BossName;

                string quality = GetQualityNameLocalized(item.Quality, _useRussian);
                string difficulty = GetDifficultyLocalized(item.Difficulty, _useRussian);

                var embed = new EmbedBuilder()
                    .WithTitle($"{GetQualityEmoji(item.Quality)} {itemName}")
                    .WithDescription($"**{(_useRussian ? "Босс" : "Boss")}:** {bossName}\n" +
                                   $"**{(_useRussian ? "Сложность" : "Difficulty")}:** {item.RaidSize} {difficulty}\n" +
                                   $"**{(_useRussian ? "Качество" : "Quality")}:** {quality}\n\n" +
                                   $"{FormatTooltip(tooltip, itemName)}")
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
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА SELECT ITEM] {ex.Message}");
                await component.FollowupAsync($"❌ Ошибка: {ex.Message}", ephemeral: true);
            }
        }
    }
}