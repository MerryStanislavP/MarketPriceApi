using MarketPriceApi.Persistence;
using MarketPriceApi.Services.Auth;
using MarketPriceApi.Services.Bars;
using MarketPriceApi.Services.Instruments;
using MarketPriceApi.Services.WebSocket;
using MarketPriceApi.Services.Sync;
using MarketPriceApi.Services.Cache;
using MarketPriceApi.Services.Assets;
using MarketPriceApi.Services.Prices;
using Microsoft.EntityFrameworkCore;
using MediatR;

namespace MarketPriceApi
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Регистрация HTTP клиента
            builder.Services.AddHttpClient();

            // Регистрация конфигурации
            // builder.Services.AddSingleton(fintaConfig);

            // Регистрация Redis кеша
            builder.Services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
                options.InstanceName = builder.Configuration["Redis:InstanceName"] ?? "MarketPriceApi:";
            });

            // Регистрация сервисов аутентификации
            builder.Services.AddSingleton<FintaAuthService>();

            // Регистрация сервисов инструментов
            builder.Services.AddScoped<GetInstrumentsQuery>(provider =>
                new GetInstrumentsQuery(
                    provider.GetRequiredService<HttpClient>(),
                    provider.GetRequiredService<FintaAuthService>(),
                    provider.GetRequiredService<IConfiguration>(),
                    provider.GetRequiredService<ILogger<GetInstrumentsQuery>>()));
            builder.Services.AddScoped<GetProvidersQuery>();
            builder.Services.AddScoped<GetExchangesQuery>();

            // Регистрация сервисов баров
            builder.Services.AddScoped<GetBarsCountBackQuery>();
            builder.Services.AddScoped<GetBarsDateRangeQuery>();
            builder.Services.AddScoped<GetBarsTimeBackQuery>();

            // Регистрация WebSocket сервиса
            builder.Services.AddSingleton<FintaWebSocketService>(provider => 
                new FintaWebSocketService(
                    provider.GetRequiredService<IConfiguration>()["Finta:WebSocketUrl"],
                    provider.GetRequiredService<IMediator>(),
                    provider.GetRequiredService<ILogger<FintaWebSocketService>>(),
                    provider.GetRequiredService<FintaAuthService>()));

            // Регистрация сервиса синхронизации
            builder.Services.AddScoped<FintaSyncService>();

            // Регистрация кеш сервиса
            builder.Services.AddScoped<PriceCacheService>();

            // Регистрация MediatR
            builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

            builder.Services.AddControllers();
            
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Добавляем поддержку WebSocket
            // builder.Services.AddWebSocketManager(); // Этот метод не существует в стандартном ASP.NET Core

            var app = builder.Build();

            // Автоматическое применение миграций
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.Migrate();
            }

            // Автоматический запуск WebSocket сервиса
            using (var scope = app.Services.CreateScope())
            {
                var webSocketService = scope.ServiceProvider.GetRequiredService<FintaWebSocketService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                try
                {
                    await webSocketService.StartAsync();
                    logger.LogInformation("WebSocket service started automatically");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to start WebSocket service automatically");
                }
            }

            // Фоновая синхронизация данных для заполнения кеша
            using (var scope = app.Services.CreateScope())
            {
                var syncService = scope.ServiceProvider.GetRequiredService<FintaSyncService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                try
                {
                    // Синхронизируем инструменты
                    var syncedCount = await syncService.SyncInstrumentsAsync("oanda", "forex");
                    logger.LogInformation("Synced {Count} instruments", syncedCount);
                    
                    // Синхронизируем цены для популярных пар
                    var popularSymbols = new[] { "EUR/USD", "GBP/USD", "USD/JPY", "USD/CHF" };
                    foreach (var symbol in popularSymbols)
                    {
                        try
                        {
                            await syncService.SyncPricesForSymbolAsync(symbol, "oanda", "1m");
                            logger.LogInformation("Synced prices for {Symbol}", symbol);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to sync prices for {Symbol}", symbol);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to perform initial data sync");
                }
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            // Добавляем поддержку WebSocket
            app.UseWebSockets();

            app.MapControllers();

            await app.RunAsync();
        }
    }
}
