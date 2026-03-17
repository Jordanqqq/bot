using System.Collections.Generic;

namespace RaidLootCore.Models
{
    public class AppSettings
    {
        // Словарь для рейдов: ключ — название ("Icecrown Citadel"), значение — 0 или 1
        public Dictionary<string, int> DownloadSettings { get; set; }

        // Глобальные настройки фильтрации
        public GlobalSettings GlobalSettings { get; set; }
    }

    public class GlobalSettings
    {
        public bool DownloadNormal { get; set; }
        public bool DownloadHeroic { get; set; }
        public bool Download10Player { get; set; }
        public bool Download25Player { get; set; }
    }
}