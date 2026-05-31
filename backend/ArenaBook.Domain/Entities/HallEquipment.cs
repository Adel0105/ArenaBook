namespace ArenaBook.Domain.Entities;

public sealed class HallEquipment
{
    public int Id { get; set; }

    public int HallId { get; set; }

    public Hall Hall { get; set; } = null!;

    public int EquipmentTypeId { get; set; }

    public EquipmentType EquipmentType { get; set; } = null!;

    public int Quantity { get; set; }
}

