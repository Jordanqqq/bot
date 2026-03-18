namespace MaNGOSExtractor.MPQ.Models;

public class SoundEntries
{
    public int Id { get; set; }
    public int SoundType { get; set; }
    public string Name { get; set; }
    public string File1 { get; set; }
    public string File2 { get; set; }
    public string File3 { get; set; }
    public string File4 { get; set; }
    public string File5 { get; set; }
    public string File6 { get; set; }
    public string File7 { get; set; }
    public string File8 { get; set; }
    public string File9 { get; set; }
    public string File10 { get; set; }
    public string DirectoryBase { get; set; }
    public float Volume { get; set; }
    public int Flags { get; set; }
    public float MinDistance { get; set; }
    public float MaxDistance { get; set; }
}