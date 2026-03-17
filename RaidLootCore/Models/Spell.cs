using System.Collections.Generic;

namespace RaidLootCore.Models
{
    public class Spell
    {
        public int Id { get; set; }
        public string NameRu { get; set; } = "";
        public string NameEn { get; set; } = "";
        public string DescriptionRu { get; set; } = "";
        public string DescriptionEn { get; set; } = "";
        public string TooltipRu { get; set; } = "";
        public string TooltipEn { get; set; } = "";

        // Базовая информация
        public int School { get; set; }          // Школа магии
        public int Category { get; set; }        // Категория заклинания
        public int CastTime { get; set; }        // Время каста в мс
        public int Duration { get; set; }        // Длительность эффекта
        public int Range { get; set; }           // Дальность
        public float Radius { get; set; }        // Радиус поражения
        public float Cooldown { get; set; }      // Кулдаун в секундах

        // Эффекты (обычно 3 эффекта на заклинание)
        public List<SpellEffect> Effects { get; set; } = new();

        // Для тактик
        public string Mechanic { get; set; } = "";      // Механика (stun, fear, root и т.д.)
        public bool IsAura { get; set; }                 // Это аура?
        public bool IsDamage { get; set; }               // Наносит урон?
        public bool IsHeal { get; set; }                 // Лечит?
        public bool IsDebuff { get; set; }               // Вредный эффект?
        public int DangerLevel { get; set; } = 1;        // 1-5 уровень опасности

        // Визуал
        public string IconName { get; set; } = "";

        // Связи с тактиками
        public string BossAbility { get; set; } = "";    // Для каких боссов
        public string WarningMessage { get; set; } = ""; // Сообщение для игрока
    }

    public class SpellEffect
    {
        public int Id { get; set; }
        public int Type { get; set; }           // Тип эффекта (6-урон, 3-хил и т.д.)
        public int DieSides { get; set; }        // Количество сторон куба
        public int BasePoints { get; set; }      // Базовое значение
        public int ChainTarget { get; set; }     // Количество цепляний
        public float Radius { get; set; }        // Радиус
        public int AuraType { get; set; }        // Тип ауры
        public int TriggerSpell { get; set; }    // Заклинание которое вызывается

        // Для текстового описания
        public string Description { get; set; } = "";
    }
}