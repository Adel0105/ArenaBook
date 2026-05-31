namespace ArenaBook.Application.Contracts.Reference;

public sealed class CreatePlatformSettingEntryRequest
{
    public string SettingKey { get; set; } = string.Empty;

    public string SettingValue { get; set; } = string.Empty;
}


