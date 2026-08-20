using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Domain.Settings;
using Microsoft.EntityFrameworkCore;

namespace CofferOS.Infrastructure.Persistence.Repositories;

public sealed class UserSettingsRepository : IUserSettingsRepository
{
    private readonly CofferOSDbContext _db;

    public UserSettingsRepository(CofferOSDbContext db) => _db = db;

    public Task<UserSettings?> GetAsync(CancellationToken ct = default) =>
        _db.UserSettings.FirstOrDefaultAsync(ct);

    public async Task AddAsync(UserSettings settings, CancellationToken ct = default) =>
        await _db.UserSettings.AddAsync(settings, ct);
}
