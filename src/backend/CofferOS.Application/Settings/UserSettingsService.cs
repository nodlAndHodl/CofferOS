using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Abstractions.Settings;
using CofferOS.Application.Contracts;
using CofferOS.Domain.Settings;

namespace CofferOS.Application.Settings;

public sealed class UserSettingsService : IUserSettingsService
{
    private readonly IUserSettingsRepository _repo;
    private readonly IUnitOfWork _unitOfWork;

    public UserSettingsService(IUserSettingsRepository repo, IUnitOfWork unitOfWork)
    {
        _repo = repo;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await _repo.GetAsync(ct);
        if (settings is null)
            return ToDto(new UserSettingsData());

        return ToDto(settings.GetData());
    }

    public async Task<UserSettingsDto> UpdateAsync(UpdateUserSettingsRequest request, CancellationToken ct = default)
    {
        var settings = await _repo.GetAsync(ct);
        var data = settings?.GetData() ?? new UserSettingsData();

        data.Currency = request.Currency;
        data.EnableLivePriceUpdates = request.EnableLivePriceUpdates;
        data.EnablePriceHistory = request.EnablePriceHistory;
        data.MempoolExplorerUrl = request.MempoolExplorerUrl;

        if (settings is null)
        {
            var newSettings = new UserSettings(data);
            await _repo.AddAsync(newSettings, ct);
        }
        else
        {
            settings.Update(data);
        }

        await _unitOfWork.SaveChangesAsync(ct);
        return ToDto(data);
    }

    private static UserSettingsDto ToDto(UserSettingsData d) =>
        new(d.Currency, d.EnableLivePriceUpdates, d.EnablePriceHistory, d.MempoolExplorerUrl);
}
