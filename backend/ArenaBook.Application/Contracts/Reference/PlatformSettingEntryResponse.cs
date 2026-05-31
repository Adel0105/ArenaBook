namespace ArenaBook.Application.Contracts.Reference;

public sealed class PlatformSettingEntryResponse
{
    public int Id { get; set; }

    public string SettingKey { get; set; } = string.Empty;

    public string SettingValue { get; set; } = string.Empty;

    public DateTime UpdatedUtc { get; set; }
}


