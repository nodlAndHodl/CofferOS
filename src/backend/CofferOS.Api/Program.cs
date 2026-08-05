using CofferOS.Api.BackgroundServices;
using CofferOS.Api.Endpoints;
using CofferOS.Api.WebSockets;
using CofferOS.Application;
using CofferOS.Application.Abstractions.Notifications;
using CofferOS.Infrastructure;
using CofferOS.Infrastructure.Persistence;
using CofferOS.Integrations.BitcoinCore;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Structured logging (console only; nothing leaves the machine).
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .Enrich.FromLogContext()
          .WriteTo.Console());

// Composition root: wire modules and integrations.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddBitcoinCoreIntegration(builder.Configuration);
builder.Services.AddElectrumServerIntegration(builder.Configuration);

// WebSocket notifications
builder.Services.AddSingleton<NotificationHub>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IWalletNotificationService, WalletNotificationService>();
builder.Services.AddScoped<ILoanNotificationService, LoanNotificationService>();

builder.Services.AddHostedService<ElectrumBlockListenerHostedService>();
builder.Services.AddHostedService<LoanDailyAccrualService>();
builder.Services.AddHostedService<DailyPriceHistoryService>();
builder.Services.AddHostedService<PriceRefreshHostedService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// The frontend is served separately (nginx / Vite). Allow it to call the API.
const string CorsPolicy = "cofferos-ui";
builder.Services.AddCors(options =>
    options.AddPolicy(CorsPolicy, policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:5173" })
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

// Apply migrations at startup so a fresh `docker compose up` just works.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CofferOSDbContext>();

    // Ensure the SQLite directory exists (SQLite will not create it).
    var dataSource = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(db.Database.GetConnectionString()).DataSource;
    var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
    if (!string.IsNullOrEmpty(directory))
        Directory.CreateDirectory(directory);

    await db.Database.MigrateAsync();
    Log.Information("Database ready at {DataSource}", dataSource);
}

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors(CorsPolicy);

app.UseWebSockets();

app.MapCofferOsEndpoints();
app.MapTreasuryEndpoints();
app.MapHoldingsEndpoints();
app.MapCostBasisEndpoints();
app.MapWebSocketEndpoints();

app.Run();
