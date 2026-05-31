namespace ArenaBook.Application.Contracts.Reference;

public sealed class UpdatePlatformSettingEntryRequest
{
    public string SettingKey { get; set; } = string.Empty;

    public string SettingValue { get; set; } = string.Empty;
}


