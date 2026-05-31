namespace ArenaBook.Domain.Entities;

public sealed class EquipmentType
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<HallEquipment> HallEquipments { get; set; } = new List<HallEquipment>();
}

