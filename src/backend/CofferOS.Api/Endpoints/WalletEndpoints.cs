using CofferOS.Application.Abstractions.Persistence;
using CofferOS.Application.Contracts;
using CofferOS.Application.Dashboard;
using CofferOS.Application.Wallets;
using CofferOS.Domain.Common;
using CofferOS.Domain.Wallets;

namespace CofferOS.Api.Endpoints;

/// <summary>Maps the HTTP surface consumed by the frontend.</summary>
public static class WalletEndpoints
{
    public static IEndpointRouteBuilder MapCofferOsEndpoints(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api");

        api.MapGet("/health", () => Results.Ok(new { status = "ok", service = "CofferOS" }))
            .WithName("Health");

        api.MapGet("/dashboard", async (DashboardService dashboard, CancellationToken ct) =>
                Results.Ok(await dashboard.GetAsync(ct)))
            .WithName("GetDashboard");

        var wallets = api.MapGroup("/wallets");

        wallets.MapGet("/", async (WalletQueryService queries, CancellationToken ct) =>
                Results.Ok(await queries.GetSummariesAsync(ct)))
            .WithName("ListWallets");

        wallets.MapGet("/{id:guid}", async (Guid id, WalletQueryService queries, CancellationToken ct) =>
            {
                var detail = await queries.GetDetailAsync(id, ct);
                return detail is null ? Results.NotFound() : Results.Ok(detail);
            })
            .WithName("GetWallet");

        wallets.MapPost("/", async (ImportWalletRequest request, WalletImportService import, CancellationToken ct) =>
            {
                try
                {
                    var created = await import.ImportAsync(request, ct);
                    return Results.Created($"/api/wallets/{created.Id}", created);
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
                catch (NotSupportedException ex)
                {
                    return Results.BadRequest(new { error = ex.Message });
                }
            })
            .WithName("ImportWallet");

        wallets.MapPost("/{id:guid}/rescan", async (Guid id, WalletRescanService rescan, CancellationToken ct) =>
            {
                try
                {
                    var result = await rescan.RescanAsync(id, ct);
                    return Results.Ok(result);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(new { error = ex.Message });
                }
            })
            .WithName("RescanWallet");

        wallets.MapDelete("/{id:guid}", async (Guid id, IWalletRepository wallets, IUnitOfWork unitOfWork, CancellationToken ct) =>
            {
                var wallet = await wallets.GetByIdAsync(id, ct);
                if (wallet is null) return Results.NotFound();
                wallets.Remove(wallet);
                await unitOfWork.SaveChangesAsync(ct);
                return Results.NoContent();
            })
            .WithName("DeleteWallet");

        wallets.MapPost("/{walletId:guid}/notes", async (Guid walletId, CreateNoteRequest request, IWalletRepository wallets, INoteRepository notes, IUnitOfWork unitOfWork, CancellationToken ct) =>
            {
                if (!Enum.TryParse<LabelTarget>(request.Target, true, out var target))
                    return Results.BadRequest(new { error = "Invalid note target." });

                var wallet = await wallets.GetByIdAsync(walletId, ct);
                if (wallet is null) return Results.NotFound();

                var note = new Note(walletId, target, request.Reference, request.Content);
                await notes.AddAsync(note, ct);
                await unitOfWork.SaveChangesAsync(ct);

                var dto = new NoteDto(note.Id, note.Target.ToString(), note.Reference, note.Content, note.CreatedAt, note.UpdatedAt);
                return Results.Created($"/api/notes/{note.Id}", dto);
            })
            .WithName("CreateNote");

        api.MapPut("/notes/{id:guid}", async (Guid id, UpdateNoteRequest request, INoteRepository notes, IUnitOfWork unitOfWork, CancellationToken ct) =>
            {
                var note = await notes.GetByIdAsync(id, ct);
                if (note is null) return Results.NotFound();

                note.Update(request.Content);
                await unitOfWork.SaveChangesAsync(ct);

                var dto = new NoteDto(note.Id, note.Target.ToString(), note.Reference, note.Content, note.CreatedAt, note.UpdatedAt);
                return Results.Ok(dto);
            })
            .WithName("UpdateNote");

        api.MapDelete("/notes/{id:guid}", async (Guid id, INoteRepository notes, IUnitOfWork unitOfWork, CancellationToken ct) =>
            {
                var note = await notes.GetByIdAsync(id, ct);
                if (note is null) return Results.NotFound();

                notes.Remove(note);
                await unitOfWork.SaveChangesAsync(ct);
                return Results.NoContent();
            })
            .WithName("DeleteNote");

        return app;
    }
}
