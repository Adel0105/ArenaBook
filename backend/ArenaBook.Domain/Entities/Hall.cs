namespace ArenaBook.Domain.Entities;

public sealed class Hall
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CityId { get; set; }

    public City City { get; set; } = null!;

    public string StreetAddress { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int CapacityPeople { get; set; }

    public decimal PricePerHourCoins { get; set; }

    public string ContactPhone { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedUtc { get; set; }

    public ICollection<HallPhoto> Photos { get; set; } = new List<HallPhoto>();

    public ICollection<HallEquipment> EquipmentLinks { get; set; } = new List<HallEquipment>();

    public ICollection<ScheduledSession> ScheduledSessions { get; set; } = new List<ScheduledSession>();

    public ICollection<HallReview> Reviews { get; set; } = new List<HallReview>();

    public ICollection<HallReaction> Reactions { get; set; } = new List<HallReaction>();
}

