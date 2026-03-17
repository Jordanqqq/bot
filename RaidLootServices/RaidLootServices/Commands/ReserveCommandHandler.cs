using Discord;
using Discord.WebSocket;
using RaidLootCore.Models;

namespace RaidLootServices
{
    public class ReserveCommandHandler : BaseCommandHandler
    {
        public ReserveCommandHandler(List<Item> database, RaidDataLoader raidLoader, RaidSessionManager sessionManager)
            : base(database, raidLoader, sessionManager) { }

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
                    string displayName = _useRussian && !string.IsNullOrEmpty(item.NameRu)
                        ? item.NameRu
                        : item.NameEn;

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

                if (session.Reserves.Count == 0)
                {
                    await command.FollowupAsync("📭 Пока нет резервов", ephemeral: true);
                    return;
                }

                var description = "";
                foreach (var reserve in session.Reserves)
                {
                    var item = _database.FirstOrDefault(x => x.Id == reserve.Key);
                    if (item != null)
                    {
                        string itemName = _useRussian && !string.IsNullOrEmpty(item.NameRu)
                            ? item.NameRu
                            : item.NameEn;
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

        public async Task HandleReserveButton(SocketMessageComponent component, int itemId)
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
                string itemName = _useRussian && !string.IsNullOrEmpty(item.NameRu)
                    ? item.NameRu
                    : item.NameEn ?? "Неизвестно";

                await component.FollowupAsync($"✅ Предмет **{itemName}** зарезервирован!", ephemeral: true);
            }
            else
            {
                await component.FollowupAsync("❌ Предмет уже зарезервирован", ephemeral: true);
            }
        }
    }
}