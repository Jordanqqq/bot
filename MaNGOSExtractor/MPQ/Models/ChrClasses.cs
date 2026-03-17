namespace MaNGOSExtractor.MPQ.Models;

public class ChrClasses
{
    public int Id { get; set; }
    public int PowerType { get; set; }
    public int PetType { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public string NameFemaleEn { get; set; }
    public string NameFemaleRu { get; set; }
    public string PetNameEn { get; set; }
    public string PetNameRu { get; set; }
}

public class ChrRaces
{
    public int Id { get; set; }
    public int Flags { get; set; }
    public int FactionId { get; set; }
    public int ExplorationSoundId { get; set; }
    public int MaleDisplayId { get; set; }
    public int FemaleDisplayId { get; set; }
    public string ClientPrefix { get; set; }
    public int CreatureType { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public string NameFemaleEn { get; set; }
    public string NameFemaleRu { get; set; }
}