using MaNGOSExtractor.MPQ.Models;
using RaidLootCore.Models; 
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace MaNGOSExtractor.Extractor.DataSaver
{
    public class JsonSaver
    {
        private readonly JsonSerializerOptions _options;

        public JsonSaver()
        {
            _options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
        }

        public async Task SaveItemsAsync(List<Item> items, string path)
        {
            string json = JsonSerializer.Serialize(items, _options);
            await File.WriteAllTextAsync(path, json);
            Console.WriteLine($"   ✅ Предметы сохранены в {path}");
        }

        public async Task SaveBossesAsync(List<Boss> bosses, string path)
        {
            string json = JsonSerializer.Serialize(bosses, _options);
            await File.WriteAllTextAsync(path, json);
            Console.WriteLine($"   ✅ Боссы сохранены в {path}");
        }


        public async Task SaveLootAsync(Dictionary<int, List<LootItem>> lootRelations, List<Item> items, List<Boss> bosses, string path)
        {
            var lootData = new List<object>();

            foreach (var relation in lootRelations)
            {
                var boss = bosses.Find(b => b.Id == relation.Key);
                if (boss == null) continue;

                var bossLoot = new
                {
                    BossId = relation.Key,
                    BossName = boss.NameEn,
                    Items = relation.Value
                };

                lootData.Add(bossLoot);
            }

            string json = JsonSerializer.Serialize(lootData, _options);
            await File.WriteAllTextAsync(path, json);
        }

        public async Task SaveSpellsAsync(List<Spell> spells, string path)
        {
            string json = JsonSerializer.Serialize(spells, _options);
            await File.WriteAllTextAsync(path, json);
            Console.WriteLine($"   ✅ Заклинания сохранены в {path}");
        }
    }
}