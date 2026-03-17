using Discord.WebSocket;

namespace RaidLootServices
{
    public class InteractionHandler
    {
        public async Task HandleMenuSelection(SocketMessageComponent component)
        {
            try
            {
                if (component.Data.CustomId == "boss_select")
                {
                    var selectedBoss = component.Data.Values.First();

                await component.RespondAsync(
                    $"Вы выбрали босса: **{selectedBoss}**",
                    ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] MenuSelection: {ex.Message}");
            }
        }

        public async Task HandleButtonClick(SocketMessageComponent component)
        {
            try
            {
                if (component.Data.CustomId.StartsWith("reserve_"))
                {
                    var itemId = component.Data.CustomId.Replace("reserve_", "");

                    await component.RespondAsync(
                        $"Вы зарезервировали предмет ID: **{itemId}**",
                        ephemeral: true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] ButtonClick: {ex.Message}");
            }
        }
    }

}
