namespace MaNGOSExtractor;

public static class RaidMapper
{
    // Сопоставление zone_id из creature → название рейда
    public static readonly Dictionary<int, (string Ru, string En)> Raids = new()
    {
        // Наксрамас (Naxxramas)
        { 3456, ("Наксрамас", "Naxxramas") },
        
        // Цитадель Ледяной Короны (Icecrown Citadel)
        { 4812, ("Цитадель Ледяной Короны", "Icecrown Citadel") },
        
        // Ульдуар (Ulduar)
        { 4273, ("Ульдуар", "Ulduar") },
        
        // Испытание крестоносца (Trial of the Crusader)
        { 4722, ("Испытание крестоносца", "Trial of the Crusader") },
        
        // Логово Ониксии (Onyxia's Lair)
        { 2159, ("Логово Ониксии", "Onyxia's Lair") },
        
        // Око Вечности (The Eye of Eternity)
        { 4500, ("Око Вечности", "The Eye of Eternity") },
        
        // Рубиновое Святилище (Ruby Sanctum)
        { 4987, ("Рубиновое Святилище", "Ruby Sanctum") },
        
        // Склеп Аркавона (Vault of Archavon)
        { 4603, ("Склеп Аркавона", "Vault of Archavon") }
    };

    // Эмодзи для боссов
    public static readonly Dictionary<string, string> BossEmoji = new()
    {
        // Lord Marrowgar / Лорд Мэрроугар
        { "Lord Marrowgar", "🦴" },
        { "Лорд Мэрроугар", "🦴" },
        
        // Lady Deathwhisper / Леди Смертный Шепот
        { "Lady Deathwhisper", "🗣️👻" },
        { "Леди Смертный Шепот", "🗣️👻" },
        
        // Deathbringer Saurfang / Саурфанг Смертоносный
        { "Deathbringer Saurfang", "🩸" },
        { "Саурфанг Смертоносный", "🩸" },
        
        // The Lich King / Король-лич
        { "The Lich King", "👑❄️💀" },
        { "Король-лич", "👑❄️💀" },
        
        // Sindragosa / Синдрагоса
        { "Sindragosa", "❄️🐲" },
        { "Синдрагоса", "❄️🐲" },
        
        // Blood Queen Lana'thel / Королева Лана'тель
        { "Blood-Queen Lana'thel", "🧛‍♀️" },
        { "Королева Лана'тель", "🧛‍♀️" },
        
        // Professor Putricide / Профессор Мерзоцид
        { "Professor Putricide", "🧪🧫" },
        { "Профессор Мерзоцид", "🧪🧫" },
        
        // Valithria Dreamwalker / Валитрия Сноходица
        { "Valithria Dreamwalker", "💚🐉" },
        { "Валитрия Сноходица", "💚🐉" }
    };
}