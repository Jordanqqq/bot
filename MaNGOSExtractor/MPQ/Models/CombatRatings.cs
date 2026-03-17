namespace MaNGOSExtractor.MPQ.Models;

public class CombatRating
{
    public int Level { get; set; }           // Уровень игрока
    public float MeleeCritRating { get; set; } // Рейтинг крита для мили
    public float RangedCritRating { get; set; } // Рейтинг крита для ренжа
    public float SpellCritRating { get; set; }  // Рейтинг крита для спеллов
    public float DodgeRating { get; set; }     // Уклонение
    public float ParryRating { get; set; }     // Парирование
    public float BlockRating { get; set; }     // Блок
    public float HitRating { get; set; }        // Меткость
    public float SpellHitRating { get; set; }   // Меткость заклинаний
    public float ResilienceRating { get; set; } // Устойчивость
    public float HasteRating { get; set; }      // Скорость
    public float SpellHasteRating { get; set; } // Скорость заклинаний
}