namespace MaNGOSExtractor.Models;

public class RawLoot
{
    public int Entry { get; set; }      // creature entry
    public int Item { get; set; }        // item entry
    public float Chance { get; set; }    // шанс дропа
}