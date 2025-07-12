using MediatR;
using MarketPriceApi.Models;
using MarketPriceApi.Persistence;
using MarketPriceApi.Services.Instruments;
using MarketPriceApi.Services.Auth;
using MarketPriceApi.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MarketPriceApi.Services.Assets
{
    public class SyncInstrumentsCommand : IRequest<int>
    {
        public string? Provider { get; set; }
        public string? Kind { get; set; }
    }

    public class SyncInstrumentsCommandHandler : IRequestHandler<SyncInstrumentsCommand, int>
    {
        private readonly AppDbContext _context;
        private readonly GetInstrumentsQuery _instrumentsQuery;
        private readonly ILogger<SyncInstrumentsCommandHandler> _logger;

        public SyncInstrumentsCommandHandler(
            AppDbContext context, 
            GetInstrumentsQuery instrumentsQuery,
            ILogger<SyncInstrumentsCommandHandler> logger)
        {
            _context = context;
            _instrumentsQuery = instrumentsQuery;
            _logger = logger;
        }

        public async Task<int> Handle(SyncInstrumentsCommand request, CancellationToken cancellationToken)
        {
            var syncLog = new SyncLog
            {
                Operation = "SyncInstruments",
                StartedAt = DateTime.UtcNow,
                Provider = request.Provider,
                Kind = request.Kind
            };

            try
            {
                _logger.LogInformation("Starting instruments synchronization for Provider: {Provider}, Kind: {Kind}", 
                    request.Provider, request.Kind);

                var page = 1;
                var totalSynced = 0;
                var hasMoreData = true;

                while (hasMoreData)
                {
                    var instrumentsResponse = await _instrumentsQuery.GetInstrumentsAsync(
                        provider: request.Provider,
                        kind: request.Kind,
                        page: page,
                        size: 100);

                    if (instrumentsResponse?.Instruments == null || !instrumentsResponse.Instruments.Any())
                    {
                        hasMoreData = false;
                        break;
                    }

                    foreach (var instrument in instrumentsResponse.Instruments)
                    {
                        var existingAsset = await _context.Assets
                            .FirstOrDefaultAsync(a => a.Symbol == instrument.Symbol, cancellationToken);

                        if (existingAsset == null)
                        {
                            // Создаем новый актив
                            var newAsset = new Asset
                            {
                                Symbol = instrument.Symbol,
                                Name = instrument.Name,
                                Provider = instrument.Provider,
                                Kind = instrument.Kind,
                                Exchange = instrument.Exchange,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow,
                                LastSyncAt = DateTime.UtcNow
                            };

                            await _context.Assets.AddAsync(newAsset, cancellationToken);
                            totalSynced++;
                        }
                        else
                        {
                            // Обновляем существующий актив
                            existingAsset.Name = instrument.Name;
                            existingAsset.Provider = instrument.Provider;
                            existingAsset.Kind = instrument.Kind;
                            existingAsset.Exchange = instrument.Exchange;
                            existingAsset.LastSyncAt = DateTime.UtcNow;

                            _context.Assets.Update(existingAsset);
                        }
                    }

                    await _context.SaveChangesAsync(cancellationToken);
                    page++;

                    // Проверяем, есть ли еще данные
                    hasMoreData = instrumentsResponse.Instruments.Count == 100;
                }

                syncLog.IsSuccess = true;
                syncLog.CompletedAt = DateTime.UtcNow;
                syncLog.RecordsProcessed = totalSynced;

                _logger.LogInformation("Instruments synchronization completed. Total synced: {TotalSynced}", totalSynced);
            }
            catch (Exception ex)
            {
                syncLog.IsSuccess = false;
                syncLog.CompletedAt = DateTime.UtcNow;
                syncLog.ErrorMessage = ex.Message;

                _logger.LogError(ex, "Error during instruments synchronization");
                throw;
            }
            finally
            {
                await _context.SyncLogs.AddAsync(syncLog, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
            }

            return syncLog.RecordsProcessed;
        }
    }
} 