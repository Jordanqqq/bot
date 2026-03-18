namespace MaNGOSExtractor.MPQ.Models;

public class CombatRating
{
    public int Level { get; set; }
    public float MeleeCrit { get; set; }
    public float RangedCrit { get; set; }
    public float SpellCrit { get; set; }
    public float Dodge { get; set; }
    public float Parry { get; set; }
    public float Block { get; set; }
    public float Hit { get; set; }
    public float SpellHit { get; set; }
    public float Resilience { get; set; }
    public float Haste { get; set; }
    public float SpellHaste { get; set; }
}