using System.Text;
using MaNGOSExtractor.MPQ.Models;

namespace MaNGOSExtractor.MPQ;

public class DbcParser
{
    private readonly MpqExtractor _mpq;
    private readonly Dictionary<string, List<object>> _cache = new();

    // Добавляем конструктор, который принимает MpqExtractor
    public DbcParser(MpqExtractor mpq)
    {
        _mpq = mpq;
    }

    // ================================================================
    // УНИВЕРСАЛЬНЫЙ ПАРСЕР ДЛЯ ЛЮБЫХ DBC
    // ================================================================

    public List<T> ParseDbcFile<T>(string dbcFileName, Func<string[], T> createRecord)
    {
        // Проверяем кэш
        var cacheKey = $"{dbcFileName}_{typeof(T).Name}";
        if (_cache.ContainsKey(cacheKey))
            return _cache[cacheKey].Cast<T>().ToList();

        var result = new List<T>();

        // Получаем файл из MPQ
        var data = _mpq.ExtractFile($"DBFilesClient\\{dbcFileName}");
        if (data == null)
        {
            Console.WriteLine($"   ❌ Файл не найден: {dbcFileName}");
            return result;
        }

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        // Заголовок DBC файла
        var magic = reader.ReadUInt32(); // Должно быть 'WDBC' (0x43424457)
        var recordCount = reader.ReadInt32();
        var fieldCount = reader.ReadInt32();
        var recordSize = reader.ReadInt32();
        var stringBlockSize = reader.ReadInt32();

        // Проверка валидности
        if (magic != 0x43424457) // 'WDBC' в little-endian
        {
            Console.WriteLine($"   ⚠️ Неверный формат DBC: {dbcFileName}");
            return result;
        }

        // Запоминаем позицию строкового блока
        var stringBlockStart = (int)ms.Position;

        // Читаем строковый блок для дальнейшего использования
        var stringBlock = reader.ReadBytes(stringBlockSize);

        // Читаем все записи
        for (int i = 0; i < recordCount; i++)
        {
            var fields = new string[fieldCount];
            var recordStart = ms.Position;

            try
            {
                for (int f = 0; f < fieldCount; f++)
                {
                    // Читаем 4 байта (в DBC все поля 4 байта)
                    var value = reader.ReadUInt32();

                    // Проверяем, является ли значение строкой (смещение в строковом блоке)
                    if (value > 0 && value < stringBlockSize)
                    {
                        // Ищем строку в строковом блоке
                        var stringPos = (int)value;
                        var strBytes = new List<byte>();

                        while (stringPos < stringBlockSize)
                        {
                            byte b = stringBlock[stringPos];
                            if (b == 0) break;
                            strBytes.Add(b);
                            stringPos++;
                        }

                        fields[f] = Encoding.UTF8.GetString(strBytes.ToArray());
                    }
                    else
                    {
                        fields[f] = value.ToString();
                    }
                }

                result.Add(createRecord(fields));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ⚠️ Ошибка парсинга записи {i} в {dbcFileName}: {ex.Message}");
                // Переходим к следующей записи
                ms.Seek(recordStart + recordSize, SeekOrigin.Begin);
                continue;
            }
        }

        // Сохраняем в кэш
        _cache[cacheKey] = result.Cast<object>().ToList();

        return result;
    }

    // ================================================================
    // ПАРСЕРЫ ДЛЯ ПРЕДМЕТОВ (Item)
    // ================================================================

    public List<ItemDisplayInfo> ParseItemDisplayInfo()
    {
        return ParseDbcFile("ItemDisplayInfo.dbc", fields => new ItemDisplayInfo
        {
            Id = int.Parse(fields[0]),
            IconName = fields.Length > 1 ? CleanIconName(fields[1]) : null,
            ModelId = fields.Length > 2 ? int.Parse(fields[2]) : 0,
            ModelId2 = fields.Length > 3 ? int.Parse(fields[3]) : 0,
            ModelId3 = fields.Length > 4 ? int.Parse(fields[4]) : 0,
            ModelId4 = fields.Length > 5 ? int.Parse(fields[5]) : 0,
            GeosetGroup = fields.Length > 6 ? int.Parse(fields[6]) : 0,
            Flags = fields.Length > 7 ? int.Parse(fields[7]) : 0,
            SpellVisualId = fields.Length > 8 ? int.Parse(fields[8]) : 0,
            GroupSoundId = fields.Length > 9 ? int.Parse(fields[9]) : 0,
            HelmetGeoset = fields.Length > 10 ? int.Parse(fields[10]) : 0,
            TextureId1 = fields.Length > 11 ? int.Parse(fields[11]) : 0,
            TextureId2 = fields.Length > 12 ? int.Parse(fields[12]) : 0,
            TextureId3 = fields.Length > 13 ? int.Parse(fields[13]) : 0,
            TextureId4 = fields.Length > 14 ? int.Parse(fields[14]) : 0,
            TextureId5 = fields.Length > 15 ? int.Parse(fields[15]) : 0,
            TextureId6 = fields.Length > 16 ? int.Parse(fields[16]) : 0,
            TextureId7 = fields.Length > 17 ? int.Parse(fields[17]) : 0,
            TextureId8 = fields.Length > 18 ? int.Parse(fields[18]) : 0,
            ItemVisual = fields.Length > 19 ? int.Parse(fields[19]) : 0,
            ParticleColorId = fields.Length > 20 ? int.Parse(fields[20]) : 0
        });
    }

    // ================================================================
    // ПАРСЕРЫ ДЛЯ СПЕЛЛОВ (Spell) - САМОЕ ВАЖНОЕ ДЛЯ ТАКТИК
    // ================================================================

    public List<Spell> ParseSpells()
    {
        return ParseDbcFile("Spell.dbc", fields => new Spell
        {
            Id = int.Parse(fields[0]),
            Category = fields.Length > 1 ? int.Parse(fields[1]) : 0,
            DispelType = fields.Length > 2 ? int.Parse(fields[2]) : 0,
            Mechanic = fields.Length > 3 ? int.Parse(fields[3]) : 0,
            Attributes = fields.Length > 4 ? int.Parse(fields[4]) : 0,
            AttributesEx = fields.Length > 5 ? int.Parse(fields[5]) : 0,
            AttributesEx2 = fields.Length > 6 ? int.Parse(fields[6]) : 0,
            AttributesEx3 = fields.Length > 7 ? int.Parse(fields[7]) : 0,
            AttributesEx4 = fields.Length > 8 ? int.Parse(fields[8]) : 0,
            AttributesEx5 = fields.Length > 9 ? int.Parse(fields[9]) : 0,
            AttributesEx6 = fields.Length > 10 ? int.Parse(fields[10]) : 0,
            AttributesEx7 = fields.Length > 11 ? int.Parse(fields[11]) : 0,

            // Школа магии (1 - физика, 2 - святость, 4 - огонь, 8 - природа, 16 - мороз, 32 - тьма, 64 - тайная)
            School = fields.Length > 12 ? int.Parse(fields[12]) : 0,

            // Косты (все 8 уровней сложности)
            CostLevel1 = fields.Length > 13 ? int.Parse(fields[13]) : 0,
            CostLevel2 = fields.Length > 14 ? int.Parse(fields[14]) : 0,
            CostLevel3 = fields.Length > 15 ? int.Parse(fields[15]) : 0,
            CostLevel4 = fields.Length > 16 ? int.Parse(fields[16]) : 0,
            CostLevel5 = fields.Length > 17 ? int.Parse(fields[17]) : 0,
            CostLevel6 = fields.Length > 18 ? int.Parse(fields[18]) : 0,
            CostLevel7 = fields.Length > 19 ? int.Parse(fields[19]) : 0,
            CostLevel8 = fields.Length > 20 ? int.Parse(fields[20]) : 0,

            // Уровни
            SpellLevel = fields.Length > 21 ? int.Parse(fields[21]) : 0,
            MaxLevel = fields.Length > 22 ? int.Parse(fields[22]) : 0,

            // Время каста и кулдауны (ID из других DBC)
            CastTimeId = fields.Length > 23 ? int.Parse(fields[23]) : 0,
            DurationId = fields.Length > 24 ? int.Parse(fields[24]) : 0,
            RangeId = fields.Length > 25 ? int.Parse(fields[25]) : 0,

            // Иконка (ID иконки)
            IconId = fields.Length > 26 ? int.Parse(fields[26]) : 0,

            // Названия (несколько языков, но мы возьмем первое)
            NameEn = fields.Length > 27 ? fields[27] : null,
            NameRu = fields.Length > 28 ? fields[28] : fields.Length > 27 ? fields[27] : null,

            // Ранг заклинания
            RankEn = fields.Length > 29 ? fields[29] : null,
            RankRu = fields.Length > 30 ? fields[30] : null,

            // Описание
            DescriptionEn = fields.Length > 31 ? fields[31] : null,
            DescriptionRu = fields.Length > 32 ? fields[32] : null,

            // Подсказка в тултипе
            ToolTipEn = fields.Length > 33 ? fields[33] : null,
            ToolTipRu = fields.Length > 34 ? fields[34] : null,

            // Мана/энергия/руны
            ManaCost = fields.Length > 35 ? int.Parse(fields[35]) : 0,
            ManaCostPerLevel = fields.Length > 36 ? int.Parse(fields[36]) : 0,
            ManaPerSecond = fields.Length > 37 ? int.Parse(fields[37]) : 0,
            ManaPerSecondPerLevel = fields.Length > 38 ? int.Parse(fields[38]) : 0,

            // Power types (0 - мана, 1 - ярость, 2 - энергия, 3 - здоровье, 6 - руны)
            PowerType = fields.Length > 39 ? int.Parse(fields[39]) : 0
        });
    }

    public List<SpellCastTime> ParseSpellCastTimes()
    {
        return ParseDbcFile("SpellCastTimes.dbc", fields => new SpellCastTime
        {
            Id = int.Parse(fields[0]),
            CastTimeMs = fields.Length > 1 ? int.Parse(fields[1]) : 0,
            CastTimePerLevel = fields.Length > 2 ? int.Parse(fields[2]) : 0
        });
    }

    public List<SpellDuration> ParseSpellDurations()
    {
        return ParseDbcFile("SpellDuration.dbc", fields => new SpellDuration
        {
            Id = int.Parse(fields[0]),
            DurationMs = fields.Length > 1 ? int.Parse(fields[1]) : 0,
            DurationPerLevel = fields.Length > 2 ? int.Parse(fields[2]) : 0,
            MaxDuration = fields.Length > 3 ? int.Parse(fields[3]) : 0
        });
    }

    public List<SpellRange> ParseSpellRanges()
    {
        return ParseDbcFile("SpellRange.dbc", fields => new SpellRange
        {
            Id = int.Parse(fields[0]),
            MinRange = fields.Length > 1 ? ParseFloat(fields[1]) : 0,
            MinRangeFriendly = fields.Length > 2 ? ParseFloat(fields[2]) : 0,
            MaxRange = fields.Length > 3 ? ParseFloat(fields[3]) : 0,
            MaxRangeFriendly = fields.Length > 4 ? ParseFloat(fields[4]) : 0,
            NameEn = fields.Length > 5 ? fields[5] : null,
            NameRu = fields.Length > 6 ? fields[6] : null,
            ShortNameEn = fields.Length > 7 ? fields[7] : null,
            ShortNameRu = fields.Length > 8 ? fields[8] : null
        });
    }

    public List<SpellIcon> ParseSpellIcons()
    {
        return ParseDbcFile("SpellIcon.dbc", fields => new SpellIcon
        {
            Id = int.Parse(fields[0]),
            IconPath = fields.Length > 1 ? CleanIconName(fields[1]) : null
        });
    }

    // ================================================================
    // ПАРСЕРЫ ДЛЯ СУЩЕСТВ (Creature)
    // ================================================================

    public List<CreatureDisplayInfo> ParseCreatureDisplayInfo()
    {
        return ParseDbcFile("CreatureDisplayInfo.dbc", fields => new CreatureDisplayInfo
        {
            Id = int.Parse(fields[0]),
            ModelId = fields.Length > 1 ? int.Parse(fields[1]) : 0,
            SoundId = fields.Length > 2 ? int.Parse(fields[2]) : 0,
            ExtendedDisplayInfoId = fields.Length > 3 ? int.Parse(fields[3]) : 0,
            CreatureModelScale = fields.Length > 4 ? ParseFloat(fields[4]) : 1.0f,
            CreatureModelAlpha = fields.Length > 5 ? int.Parse(fields[5]) : 255,
            IconName = fields.Length > 6 ? CleanIconName(fields[6]) : null,
            PortraitTextureId = fields.Length > 7 ? int.Parse(fields[7]) : 0,
            BloodLevel = fields.Length > 8 ? int.Parse(fields[8]) : 0,
            BloodId = fields.Length > 9 ? int.Parse(fields[9]) : 0,
            ParticleColorId = fields.Length > 10 ? int.Parse(fields[10]) : 0,
            CreatureGeosetData = fields.Length > 11 ? int.Parse(fields[11]) : 0,
            ObjectEffectPackageId = fields.Length > 12 ? int.Parse(fields[12]) : 0
        });
    }

    public List<CreatureFamily> ParseCreatureFamily()
    {
        return ParseDbcFile("CreatureFamily.dbc", fields => new CreatureFamily
        {
            Id = int.Parse(fields[0]),
            MinScale = fields.Length > 1 ? ParseFloat(fields[1]) : 0,
            MinScaleLevel = fields.Length > 2 ? int.Parse(fields[2]) : 0,
            MaxScale = fields.Length > 3 ? ParseFloat(fields[3]) : 0,
            MaxScaleLevel = fields.Length > 4 ? int.Parse(fields[4]) : 0,
            SkillLineId = fields.Length > 5 ? int.Parse(fields[5]) : 0,
            PetFoodMask = fields.Length > 6 ? int.Parse(fields[6]) : 0,
            PetTalentType = fields.Length > 7 ? int.Parse(fields[7]) : 0,
            CategoryEnumId = fields.Length > 8 ? int.Parse(fields[8]) : 0,
            NameEn = fields.Length > 9 ? fields[9] : null,
            NameRu = fields.Length > 10 ? fields[10] : null,
            IconFile = fields.Length > 11 ? fields[11] : null
        });
    }

    public List<CreatureType> ParseCreatureType()
    {
        return ParseDbcFile("CreatureType.dbc", fields => new CreatureType
        {
            Id = int.Parse(fields[0]),
            NameEn = fields.Length > 1 ? fields[1] : null,
            NameRu = fields.Length > 2 ? fields[2] : null
        });
    }

    // ================================================================
    // ПАРСЕРЫ ДЛЯ КАРТ И ЗОН (Map & Area)
    // ================================================================

    public List<Map> ParseMaps()
    {
        return ParseDbcFile("Map.dbc", fields => new Map
        {
            Id = int.Parse(fields[0]),
            Directory = fields.Length > 1 ? fields[1] : null,
            MapType = fields.Length > 2 ? int.Parse(fields[2]) : 0,
            IsPVP = fields.Length > 3 ? int.Parse(fields[3]) == 1 : false,
            IsRaid = fields.Length > 4 ? int.Parse(fields[4]) == 1 : false,
            NameEn = fields.Length > 5 ? fields[5] : null,
            NameRu = fields.Length > 6 ? fields[6] : null,
            MinLevel = fields.Length > 7 ? int.Parse(fields[7]) : 0,
            MaxLevel = fields.Length > 8 ? int.Parse(fields[8]) : 0,
            MaxPlayers = fields.Length > 9 ? int.Parse(fields[9]) : 0
        });
    }

    public List<AreaTable> ParseAreas()
    {
        return ParseDbcFile("AreaTable.dbc", fields => new AreaTable
        {
            Id = int.Parse(fields[0]),
            MapId = fields.Length > 1 ? int.Parse(fields[1]) : 0,
            ZoneId = fields.Length > 2 ? int.Parse(fields[2]) : 0,
            ExploreFlag = fields.Length > 3 ? int.Parse(fields[3]) : 0,
            Flags = fields.Length > 4 ? int.Parse(fields[4]) : 0,
            SoundPreferences = fields.Length > 5 ? int.Parse(fields[5]) : 0,
            SoundPreferences2 = fields.Length > 6 ? int.Parse(fields[6]) : 0,
            SoundPreferences3 = fields.Length > 7 ? int.Parse(fields[7]) : 0,
            SoundPreferences4 = fields.Length > 8 ? int.Parse(fields[8]) : 0,
            AreaLevel = fields.Length > 9 ? int.Parse(fields[9]) : 0,
            NameEn = fields.Length > 10 ? fields[10] : null,
            NameRu = fields.Length > 11 ? fields[11] : null,
            Team = fields.Length > 12 ? int.Parse(fields[12]) : 0,
            LiquidOverride = fields.Length > 13 ? int.Parse(fields[13]) : 0,
            MinElevation = fields.Length > 14 ? ParseFloat(fields[14]) : 0,
            AmbientMultiplier = fields.Length > 15 ? ParseFloat(fields[15]) : 0,
            LightId = fields.Length > 16 ? int.Parse(fields[16]) : 0
        });
    }

    public List<DungeonEncounter> ParseDungeonEncounters()
    {
        return ParseDbcFile("DungeonEncounter.dbc", fields => new DungeonEncounter
        {
            Id = int.Parse(fields[0]),
            MapId = fields.Length > 1 ? int.Parse(fields[1]) : 0,
            Difficulty = fields.Length > 2 ? int.Parse(fields[2]) : 0,
            OrderIndex = fields.Length > 3 ? int.Parse(fields[3]) : 0,
            CreatureId = fields.Length > 4 ? int.Parse(fields[4]) : 0,
            NameEn = fields.Length > 5 ? fields[5] : null,
            NameRu = fields.Length > 6 ? fields[6] : null,
            SpellIconId = fields.Length > 7 ? int.Parse(fields[7]) : 0,
            Flags = fields.Length > 8 ? int.Parse(fields[8]) : 0
        });
    }

    // ================================================================
    // ПАРСЕРЫ ДЛЯ КЛАССОВ И РАС (Class & Race)
    // ================================================================

    public List<ChrClasses> ParseClasses()
    {
        return ParseDbcFile("ChrClasses.dbc", fields => new ChrClasses
        {
            Id = int.Parse(fields[0]),
            PowerType = fields.Length > 1 ? int.Parse(fields[1]) : 0,
            PetType = fields.Length > 2 ? int.Parse(fields[2]) : 0,
            NameEn = fields.Length > 3 ? fields[3] : null,
            NameRu = fields.Length > 4 ? fields[4] : null,
            NameFemaleEn = fields.Length > 5 ? fields[5] : null,
            NameFemaleRu = fields.Length > 6 ? fields[6] : null,
            PetNameEn = fields.Length > 7 ? fields[7] : null,
            PetNameRu = fields.Length > 8 ? fields[8] : null
        });
    }

    public List<ChrRaces> ParseRaces()
    {
        return ParseDbcFile("ChrRaces.dbc", fields => new ChrRaces
        {
            Id = int.Parse(fields[0]),
            Flags = fields.Length > 1 ? int.Parse(fields[1]) : 0,
            FactionId = fields.Length > 2 ? int.Parse(fields[2]) : 0,
            ExplorationSoundId = fields.Length > 3 ? int.Parse(fields[3]) : 0,
            MaleDisplayId = fields.Length > 4 ? int.Parse(fields[4]) : 0,
            FemaleDisplayId = fields.Length > 5 ? int.Parse(fields[5]) : 0,
            ClientPrefix = fields.Length > 6 ? fields[6] : null,
            CreatureType = fields.Length > 7 ? int.Parse(fields[7]) : 0,
            NameEn = fields.Length > 8 ? fields[8] : null,
            NameRu = fields.Length > 9 ? fields[9] : null,
            NameFemaleEn = fields.Length > 10 ? fields[10] : null,
            NameFemaleRu = fields.Length > 11 ? fields[11] : null
        });
    }

    // ================================================================
    // ПАРСЕРЫ ДЛЯ СТАТОВ И РЕЙТИНГОВ (Formulas)
    // ================================================================

    public List<CombatRating> ParseCombatRatings()
    {
        var result = new List<CombatRating>();

        // GtCombatRatings.dbc - особый формат (нестандартный)
        var data = _mpq.ExtractFile("DBFilesClient\\GtCombatRatings.dbc");
        if (data == null)
            return result;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        // Пропускаем заголовок (20 байт для GtCombatRatings)
        reader.ReadBytes(20);

        // Читаем данные для уровней 1-80 (в WotLK максимальный уровень 80)
        for (int i = 1; i <= 80; i++)
        {
            try
            {
                var rating = new CombatRating
                {
                    Level = i,
                    MeleeCrit = reader.ReadSingle(),
                    RangedCrit = reader.ReadSingle(),
                    SpellCrit = reader.ReadSingle(),
                    Dodge = reader.ReadSingle(),
                    Parry = reader.ReadSingle(),
                    Block = reader.ReadSingle(),
                    Hit = reader.ReadSingle(),
                    SpellHit = reader.ReadSingle(),
                    Resilience = reader.ReadSingle(),
                    Haste = reader.ReadSingle(),
                    SpellHaste = reader.ReadSingle()
                };
                result.Add(rating);
            }
            catch
            {
                break;
            }
        }

        return result;
    }

    // ================================================================
    // ПАРСЕРЫ ДЛЯ ЗВУКОВ (Sound)
    // ================================================================

    public List<SoundEntries> ParseSoundEntries()
    {
        return ParseDbcFile("SoundEntries.dbc", fields => new SoundEntries
        {
            Id = int.Parse(fields[0]),
            SoundType = fields.Length > 1 ? int.Parse(fields[1]) : 0,
            Name = fields.Length > 2 ? fields[2] : null,
            File1 = fields.Length > 3 ? fields[3] : null,
            File2 = fields.Length > 4 ? fields[4] : null,
            File3 = fields.Length > 5 ? fields[5] : null,
            File4 = fields.Length > 6 ? fields[6] : null,
            File5 = fields.Length > 7 ? fields[7] : null,
            File6 = fields.Length > 8 ? fields[8] : null,
            File7 = fields.Length > 9 ? fields[9] : null,
            File8 = fields.Length > 10 ? fields[10] : null,
            File9 = fields.Length > 11 ? fields[11] : null,
            File10 = fields.Length > 12 ? fields[12] : null,
            DirectoryBase = fields.Length > 13 ? fields[13] : null,
            Volume = fields.Length > 14 ? ParseFloat(fields[14]) : 1.0f,
            Flags = fields.Length > 15 ? int.Parse(fields[15]) : 0,
            MinDistance = fields.Length > 16 ? ParseFloat(fields[16]) : 0,
            MaxDistance = fields.Length > 17 ? ParseFloat(fields[17]) : 0
        });
    }

    // ================================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ================================================================

    private float ParseFloat(string value)
    {
        if (float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;
        return 0;
    }

    private string CleanIconName(string iconName)
    {
        if (string.IsNullOrEmpty(iconName))
            return null;

        // Убираем путь и расширение
        var name = Path.GetFileNameWithoutExtension(iconName);
        // Заменяем обратные слеши на прямые
        return name.Replace('\\', '/').ToLowerInvariant();
    }

    // Создание карты displayId -> iconName для быстрого поиска
    public Dictionary<int, string> GetItemIconMap()
    {
        var items = ParseItemDisplayInfo();
        return items
            .Where(x => !string.IsNullOrEmpty(x.IconName))
            .ToDictionary(x => x.Id, x => x.IconName);
    }

    // Создание карты spellId -> iconName
    public Dictionary<int, string> GetSpellIconMap()
    {
        var spells = ParseSpells();
        var icons = ParseSpellIcons();
        var iconDict = icons.ToDictionary(x => x.Id, x => x.IconPath);

        return spells
            .Where(x => iconDict.ContainsKey(x.IconId))
            .ToDictionary(x => x.Id, x => iconDict[x.IconId]);
    }

    // Создание карты creatureId -> iconName
    public Dictionary<int, string> GetCreatureIconMap()
    {
        var creatures = ParseCreatureDisplayInfo();
        return creatures
            .Where(x => !string.IsNullOrEmpty(x.IconName))
            .ToDictionary(x => x.Id, x => x.IconName);
    }

    // Очистка кэша
    public void ClearCache()
    {
        _cache.Clear();
    }
}