using RaidLootCore.Models;
using System.Collections.Generic;
using System.Linq;  

namespace MaNGOSExtractor.Extractor.Parsers
{
    public class BossParser
    {
        public (int total, int highHealth, int averageLevel) AnalyzeBosses(List<Boss> bosses)
        {
            int total = bosses.Count;
            int highHealth = bosses.Count(b => b.Health > 1000000);
            int averageLevel = bosses.Count > 0 ? (int)(bosses.Sum(b => b.Level) / bosses.Count) : 0;

            return (total, highHealth, averageLevel);
        }
    }
}