using System.Collections.Generic;
using RaidLootCore.Models;

namespace MaNGOSExtractor.Extractor.Parsers
{
    public class SpellAnalyzer
    {
        public void ClassifySpells(List<Spell> spells)
        {
            foreach (var spell in spells)
            {
                // Простая классификация по названию
                string name = spell.NameEn.ToLower();

                if (name.Contains("damage") || name.Contains("strike") || name.Contains("smash"))
                    spell.IsDamage = true;

                if (name.Contains("heal") || name.Contains("cure") || name.Contains("renew"))
                    spell.IsHeal = true;

                if (name.Contains("debuff") || name.Contains("curse") || name.Contains("poison"))
                    spell.IsDebuff = true;
            }
        }
    }
}