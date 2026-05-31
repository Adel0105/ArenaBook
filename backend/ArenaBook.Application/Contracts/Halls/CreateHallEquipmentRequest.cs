namespace ArenaBook.Application.Contracts.Halls;

public sealed class CreateHallEquipmentRequest
{
    public int EquipmentTypeId { get; set; }

    public int Quantity { get; set; }
}


