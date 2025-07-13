using MediatR;
using MarketPriceApi.Models.DTOs;
using MarketPriceApi.Services.Prices;

namespace MarketPriceApi.Services.Prices
{
    public class GetMultiplePricesQuery : IRequest<List<CurrentPriceDto>>
    {
        public List<string> Symbols { get; set; } = new List<string>();
        public string Provider { get; set; } = string.Empty;
        public string Interval { get; set; } = "1m";
    }

    public class GetMultiplePricesQueryHandler : IRequestHandler<GetMultiplePricesQuery, List<CurrentPriceDto>>
    {
        private readonly IMediator _mediator;

        public GetMultiplePricesQueryHandler(IMediator mediator)
        {
            _mediator = mediator;
        }

        public async Task<List<CurrentPriceDto>> Handle(GetMultiplePricesQuery request, CancellationToken cancellationToken)
        {
            var tasks = request.Symbols.Select(symbol => 
                _mediator.Send(new GetCurrentPriceQuery
                {
                    Symbol = symbol,
                    Provider = request.Provider,
                    Interval = request.Interval
                }, cancellationToken));

            var results = await Task.WhenAll(tasks);
            return results.Where(r => r != null).ToList()!;
        }
    }
} 