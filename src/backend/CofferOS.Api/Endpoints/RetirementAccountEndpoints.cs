using CofferOS.Application.Contracts;
using CofferOS.Application.Retirement;

namespace CofferOS.Api.Endpoints;

public static class RetirementAccountEndpoints
{
    public static IEndpointRouteBuilder MapRetirementAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/retirement-accounts");

        api.MapPost("/", CreateRetirementAccount)
            .WithName("CreateRetirementAccount");

        api.MapGet("/", GetAllRetirementAccounts)
            .WithName("GetAllRetirementAccounts");

        api.MapGet("/{id:guid}", GetRetirementAccountById)
            .WithName("GetRetirementAccountById");

        api.MapPut("/{id:guid}", UpdateRetirementAccount)
            .WithName("UpdateRetirementAccount");

        api.MapDelete("/{id:guid}", DeleteRetirementAccount)
            .WithName("DeleteRetirementAccount");

        api.MapPost("/{id:guid}/cost-basis", AddCostBasisEntry)
            .WithName("AddCostBasisEntry");

        api.MapDelete("/{id:guid}/cost-basis/{entryId:guid}", RemoveCostBasisEntry)
            .WithName("RemoveCostBasisEntry");

        return app;
    }

    private static async Task<IResult> CreateRetirementAccount(
        CreateRetirementAccountRequest request,
        RetirementAccountService service,
        CancellationToken ct)
    {
        var created = await service.CreateAsync(request, ct);
        return Results.Created($"/api/retirement-accounts/{created.Id}", created);
    }

    private static async Task<IResult> GetAllRetirementAccounts(
        RetirementAccountService service,
        CancellationToken ct) =>
        Results.Ok(await service.GetAllAsync(ct));

    private static async Task<IResult> GetRetirementAccountById(
        Guid id,
        RetirementAccountService service,
        CancellationToken ct)
    {
        var account = await service.GetByIdAsync(id, ct);
        return account is null ? Results.NotFound() : Results.Ok(account);
    }

    private static async Task<IResult> UpdateRetirementAccount(
        Guid id,
        UpdateRetirementAccountRequest request,
        RetirementAccountService service,
        CancellationToken ct)
    {
        try
        {
            var updated = await service.UpdateAsync(id, request, ct);
            return Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> DeleteRetirementAccount(
        Guid id,
        RetirementAccountService service,
        CancellationToken ct)
    {
        var deleted = await service.DeleteAsync(id, ct);
        return deleted ? Results.NoContent() : Results.NotFound();
    }

    private static async Task<IResult> AddCostBasisEntry(
        Guid id,
        CostBasisEntryInput entry,
        RetirementAccountService service,
        CancellationToken ct)
    {
        try
        {
            var updated = await service.AddCostBasisEntryAsync(id, entry, ct);
            return Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> RemoveCostBasisEntry(
        Guid id,
        Guid entryId,
        RetirementAccountService service,
        CancellationToken ct)
    {
        try
        {
            var updated = await service.RemoveCostBasisEntryAsync(id, entryId, ct);
            return Results.Ok(updated);
        }
        catch (InvalidOperationException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }
}
