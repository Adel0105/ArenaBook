namespace ArenaBook.Application.Contracts.Halls;

public sealed class HallEquipmentResponse
{
    public int Id { get; set; }

    public int HallId { get; set; }

    public int EquipmentTypeId { get; set; }

    public string EquipmentTypeName { get; set; } = string.Empty;

    public int Quantity { get; set; }
}


