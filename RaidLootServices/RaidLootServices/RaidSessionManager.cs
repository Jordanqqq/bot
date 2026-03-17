using System.Collections.Generic;

namespace RaidLootServices
{
    public class RaidSession
    {
        public ulong LeaderId { get; set; }
        public string RaidName { get; set; } = "";
        public string Difficulty { get; set; } = "";
        public string CurrentBoss { get; set; } = "";
        // Добавляем словарь для хранения резервов
        public Dictionary<int, ulong> Reserves { get; set; } = new();
    }

    public class RaidSessionManager
    {
        private readonly Dictionary<ulong, RaidSession> _sessions = new();

        public RaidSession? GetSession(ulong userId)
        {
            return _sessions.GetValueOrDefault(userId);
        }

        public RaidSession CreateSession(ulong userId, string raidName, string difficulty)
        {
            var session = new RaidSession
            {
                LeaderId = userId,
                RaidName = raidName,
                Difficulty = difficulty,
                Reserves = new Dictionary<int, ulong>() // Инициализируем словарь
            };
            _sessions[userId] = session;
            return session;
        }

        // Метод для добавления резерва
        public bool TryReserve(ulong userId, int itemId, ulong playerId)
        {
            var session = GetSession(userId);
            if (session == null) return false;

            // Проверяем, не зарезервирован ли уже предмет
            if (session.Reserves.ContainsKey(itemId)) return false;

            // Добавляем резерв
            session.Reserves[itemId] = playerId;
            return true;
        }

        // Метод для получения всех резервов
        public Dictionary<int, ulong> GetReserves(ulong userId)
        {
            var session = GetSession(userId);
            return session?.Reserves ?? new Dictionary<int, ulong>();
        }
    }
}