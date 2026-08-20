using System.Text.Json;

namespace CofferOS.Domain.Settings;

/// <summary>
/// Singleton user-configurable settings stored as a JSON blob.
/// One row per application instance; extended over time with new keys.
/// </summary>
public sealed class UserSettings
{
    public static readonly Guid SingletonId = new("00000000-0000-0000-0000-000000000001");

    private static readonly JsonSerializerOptions _jsonOpts = new() { PropertyNameCaseInsensitive = true };

    private UserSettings() { }

    public UserSettings(UserSettingsData data)
    {
        Id = SingletonId;
        SettingsJson = JsonSerializer.Serialize(data);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; } = SingletonId;
    public string SettingsJson { get; private set; } = "{}";
    public DateTimeOffset UpdatedAt { get; private set; }

    public UserSettingsData GetData() =>
        JsonSerializer.Deserialize<UserSettingsData>(SettingsJson, _jsonOpts) ?? new UserSettingsData();

    public void Update(UserSettingsData data)
    {
        SettingsJson = JsonSerializer.Serialize(data);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

/// <summary>
/// Typed settings payload. New fields can be added here without schema migrations
/// because the entire object is stored as a JSON blob.
/// </summary>
public sealed class UserSettingsData
{
    public string Currency { get; set; } = "USD";
    public bool EnableLivePriceUpdates { get; set; } = true;
    public bool EnablePriceHistory { get; set; } = true;
    public string? MempoolExplorerUrl { get; set; }
}
