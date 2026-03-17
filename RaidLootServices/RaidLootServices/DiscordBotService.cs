using Discord;
using Discord.WebSocket;
using System.Text.Json;
using RaidLootCore.Models;
using RaidLootServices;

namespace RaidLootServices
{
    public class DiscordBotService
    {
        private DiscordSocketClient _client;
        private List<Item> _masterDatabase;
        private CommandHandler _commandHandler;
        private RaidSessionManager _sessionManager;
        private RaidDataLoader _raidLoader;
        private ulong _targetChannelId;

        public async Task StartWithDataAsync(string token, List<Item> items, RaidDataLoader raidLoader, RaidSessionManager sessionManager, ulong channelId)
        {
            _targetChannelId = channelId;
            _masterDatabase = items;
            _raidLoader = raidLoader;
            _sessionManager = sessionManager;

            Console.WriteLine($"📦 Текущая база: {_masterDatabase.Count} предметов (будет пополняться)");

            _client = new DiscordSocketClient(
                new DiscordSocketConfig
                {
                    GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
                });

            _commandHandler = new CommandHandler(_masterDatabase, _raidLoader, _sessionManager);

            _client.Log += LogAsync;
            _client.Ready += OnReadyAsync;
            _client.SlashCommandExecuted += HandleSlashCommand;
            _client.SelectMenuExecuted += HandleAllMenus;

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Периодически показываем статус загрузки
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(30000); // Каждые 30 секунд
                    Console.WriteLine($"📊 Статус базы: {_masterDatabase.Count} предметов");
                }
            });

            await Task.Delay(-1);
        }

        // Единый обработчик для всех меню
        private async Task HandleAllMenus(SocketMessageComponent component)
        {
            try
            {
                Console.WriteLine($"[MENU] Получен CustomId: {component.Data.CustomId}");

                // Сначала пробуем обработать как меню рейда
                if (component.Data.CustomId == "select_raid")
                {
                    await _commandHandler.HandleSelectRaid(component);
                }
                // Затем как общее меню (боссы, предметы)
                else
                {
                    await _commandHandler.HandleMenuSelection(component);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА MENU] {ex.Message}");
                try
                {
                    await component.RespondAsync($"❌ Ошибка: {ex.Message}", ephemeral: true);
                }
                catch { }
            }
        }

        private async Task HandleSlashCommand(SocketSlashCommand command)
        {
            try
            {
                Console.WriteLine($"[SLASH] Команда: {command.CommandName} от {command.User.Username}");

                switch (command.CommandName)
                {
                    case "loot":
                        await _commandHandler.HandleLootCommand(command);
                        break;

                    case "raid":
                        await _commandHandler.HandleRaidCommand(command);
                        break;

                    case "create_raid":
                        await _commandHandler.HandleCreateRaidCommand(command);
                        break;

                    case "add_reserve":
                        await _commandHandler.HandleAddReserveCommand(command);
                        break;

                    case "show_reserves":
                        await _commandHandler.HandleShowReservesCommand(command);
                        break;

                    default:
                        await command.RespondAsync("❌ Неизвестная команда", ephemeral: true);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ОШИБКА SLASH] {ex.Message}");
                try
                {
                    await command.RespondAsync($"❌ Ошибка: {ex.Message}", ephemeral: true);
                }
                catch { }
            }
        }

        private async Task OnReadyAsync()
        {
            var guild = _client.Guilds.FirstOrDefault();

            if (guild != null)
            {
                try
                {
                    // Удаляем старые команды (чтобы избежать дублирования)
                    var existingCommands = await guild.GetApplicationCommandsAsync();
                    foreach (var cmd in existingCommands)
                    {
                        await cmd.DeleteAsync();
                    }

                    // команда /loot
                    var lootCommand = new SlashCommandBuilder()
                        .WithName("loot")
                        .WithDescription("Поиск предмета")
                        .AddOption("название",
                            ApplicationCommandOptionType.String,
                            "Введите название предмета",
                            isRequired: true);

                    await guild.CreateApplicationCommandAsync(lootCommand.Build());

                    // команда /raid
                    var raidCommand = new SlashCommandBuilder()
                        .WithName("raid")
                        .WithDescription("Посмотреть лут рейда");

                    await guild.CreateApplicationCommandAsync(raidCommand.Build());

                    // команда create_raid
                    var createRaidCommand = new SlashCommandBuilder()
                        .WithName("create_raid")
                        .WithDescription("Создать новый рейд (для рейд-лидера)")
                        .AddOption("название", ApplicationCommandOptionType.String, "Название рейда", isRequired: true)
                        .AddOption("сложность", ApplicationCommandOptionType.String, "10/25/Героик", isRequired: true);

                    await guild.CreateApplicationCommandAsync(createRaidCommand.Build());

                    // команда add_reserve
                    var addReserveCommand = new SlashCommandBuilder()
                        .WithName("add_reserve")
                        .WithDescription("Зарезервировать предмет")
                        .AddOption("предмет", ApplicationCommandOptionType.String, "Название предмета", isRequired: true);

                    await guild.CreateApplicationCommandAsync(addReserveCommand.Build());

                    // команда show_reserves
                    var showReservesCommand = new SlashCommandBuilder()
                        .WithName("show_reserves")
                        .WithDescription("Показать все резервы текущего рейда");

                    await guild.CreateApplicationCommandAsync(showReservesCommand.Build());

                    Console.WriteLine("✅ Slash команды зарегистрированы.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Ошибка регистрации команд: {ex.Message}");
                }
            }
        }

        private async Task LoadAllDataAsync(string rootPath)
        {
            if (!Directory.Exists(rootPath))
            {
                Directory.CreateDirectory(rootPath);
                return;
            }

            var files = Directory.GetFiles(rootPath, "*.json", SearchOption.AllDirectories);

            _masterDatabase.Clear();

            foreach (var file in files)
            {
                try
                {
                    using FileStream openStream = File.OpenRead(file);
                    var items = await JsonSerializer.DeserializeAsync<List<Item>>(openStream);

                    if (items != null)
                        _masterDatabase.AddRange(items);

                    Console.WriteLine($"[БАЗА] Загружено {items?.Count ?? 0} предметов из {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки {file}: {ex.Message}");
                }
            }

            Console.WriteLine($"[БАЗА] ВСЕГО ЗАГРУЖЕНО: {_masterDatabase.Count} предметов");
        }

        private Task LogAsync(LogMessage msg)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg.Source}: {msg.Message}");
            return Task.CompletedTask;
        }
    }
}