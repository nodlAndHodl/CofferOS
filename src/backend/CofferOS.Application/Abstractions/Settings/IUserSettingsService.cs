using CofferOS.Application.Contracts;

namespace CofferOS.Application.Abstractions.Settings;

public interface IUserSettingsService
{
    Task<UserSettingsDto> GetAsync(CancellationToken ct = default);
    Task<UserSettingsDto> UpdateAsync(UpdateUserSettingsRequest request, CancellationToken ct = default);
}
