namespace ArenaBook.Domain.Entities;

public sealed class PlatformSettingEntry
{
    public int Id { get; set; }

    public string SettingKey { get; set; } = string.Empty;

    public string SettingValue { get; set; } = string.Empty;

    public DateTime UpdatedUtc { get; set; }
}

