namespace MaNGOSExtractor.Services;

public class ItemFilter
{
    // Качество предметов в MaNGOS
    // 0 = Серый (мусор)
    // 1 = Белый (обычный)
    // 2 = Зеленый (необычный)
    // 3 = Синий (редкий)
    // 4 = Фиолетовый (эпический)
    // 5 = Оранжевый (легендарный)

    public bool IsEpicOrLegendary(int quality)
    {
        return quality == 4 || quality == 5;
    }

    public bool IsRareOrBetter(int quality)
    {
        return quality >= 3;
    }

    public string GetQualityName(int quality)
    {
        return quality switch
        {
            0 => "Мусорный",
            1 => "Обычный",
            2 => "Необычный",
            3 => "Редкий",
            4 => "Эпический",
            5 => "Легендарный",
            _ => "Неизвестный"
        };
    }
}