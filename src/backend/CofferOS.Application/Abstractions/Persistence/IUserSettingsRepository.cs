using CofferOS.Domain.Settings;

namespace CofferOS.Application.Abstractions.Persistence;

public interface IUserSettingsRepository
{
    Task<UserSettings?> GetAsync(CancellationToken ct = default);
    Task AddAsync(UserSettings settings, CancellationToken ct = default);
}
