namespace MaNGOSExtractor.MPQ.Models;

public class CreatureDisplayInfo
{
    public int Id { get; set; }
    public int ModelId { get; set; }
    public int SoundId { get; set; }
    public int ExtendedDisplayInfoId { get; set; }
    public float CreatureModelScale { get; set; }
    public int CreatureModelAlpha { get; set; }
    public string IconName { get; set; }
    public int PortraitTextureId { get; set; }
    public int BloodLevel { get; set; }
    public int BloodId { get; set; }
    public int ParticleColorId { get; set; }
    public int CreatureGeosetData { get; set; }
    public int ObjectEffectPackageId { get; set; }
}

public class CreatureFamily
{
    public int Id { get; set; }
    public float MinScale { get; set; }
    public int MinScaleLevel { get; set; }
    public float MaxScale { get; set; }
    public int MaxScaleLevel { get; set; }
    public int SkillLineId { get; set; }
    public int PetFoodMask { get; set; }
    public int PetTalentType { get; set; }
    public int CategoryEnumId { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
    public string IconFile { get; set; }
}

public class CreatureType
{
    public int Id { get; set; }
    public string NameEn { get; set; }
    public string NameRu { get; set; }
}