using BusinessLogic.Interfaces;
using BusinessLogic.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ArbitrageBot.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CryptoController(ILogger<CryptoController> logger, IEnumerable<ICryptoExchangeApiService> cryptoApiServices, IConfiguration configuration) : ControllerBase
{
    [HttpGet("prices")]
    public async Task<IActionResult> GetPrices([FromQuery] ushort minPercent, [FromQuery] ushort maxPercent, [FromQuery] string? filterTicket, CancellationToken cancellationToken)
    {
        var sw = new Stopwatch();
        sw.Start();

        var tasks = cryptoApiServices.Select(service => service.GetPricesAsync(cancellationToken));
        var prices = await Task.WhenAll(tasks);

        logger.LogInformation($"WhenAll ElapsedMilliseconds:{sw.ElapsedMilliseconds}");
        sw.Restart();

        var results = new List<(CryptoPrice LowPrice, CryptoPrice BigPrice, decimal Percent)>();

        var allowedExchanges = configuration.GetSection("allowedExchanges").Get<ExchangeType[]>()!;

        var groups = prices
            //.AsParallel()
            .SelectMany(x => x)
            .Where(x => allowedExchanges.Contains(x.Type))
            .GroupBy(t => t.Symbol)
            .Where(g => g.Count() > 1);

        foreach (var group in groups)
        {
            var tickets = group.Where(x => x.LastPrice != 0);

            foreach (var theLowestPriceTicket in tickets)
            {
                foreach (var ticket in tickets.Where(x => x.Type != theLowestPriceTicket.Type))
                {
                    if (ticket.LastPrice == 0 || theLowestPriceTicket.LastPrice == 0) continue;

                    var percent = theLowestPriceTicket.LastPrice.PercentOf(minPercent);
                    if (ticket.LastPrice > theLowestPriceTicket.LastPrice.PercentOf(minPercent) + theLowestPriceTicket.LastPrice
                        && ticket.LastPrice < theLowestPriceTicket.LastPrice.PercentOf(maxPercent) + theLowestPriceTicket.LastPrice)
                    {
                        var diffPercent = ((ticket.LastPrice - theLowestPriceTicket.LastPrice) / theLowestPriceTicket.LastPrice) * 100;
                        results.Add((theLowestPriceTicket, ticket, diffPercent));
                    }
                }
            }
            //var theLowestPriceTiket = tickets.MinBy(p => p.LastPrice)!;
            //foreach (var tiket in tickets.Where(x => x.Type != theLowestPriceTiket.Type))
            //{
            //    if (tiket.LastPrice == 0 || theLowestPriceTiket.LastPrice == 0)
            //    {
            //        continue;
            //    }


            //    if (tiket.LastPrice > theLowestPriceTiket.LastPrice.PercentOf(minPercent) + theLowestPriceTiket.LastPrice
            //        && tiket.LastPrice < theLowestPriceTiket.LastPrice.PercentOf(maxPercent) + theLowestPriceTiket.LastPrice)
            //    {
            //        var diffPercent = ((tiket.LastPrice - theLowestPriceTiket.LastPrice) / theLowestPriceTiket.LastPrice) * 100;
            //        results.Add((theLowestPriceTiket, tiket, diffPercent.ToString()));
            //    }
            //}
        }

        logger.LogInformation($"Foreach ElapsedMilliseconds:{sw.ElapsedMilliseconds}");

        return Ok(results
            .Select(r => new { r.LowPrice, r.BigPrice, Percent = Math.Round(r.Percent, 3, MidpointRounding.ToEven) })
            .Where(r => string.IsNullOrWhiteSpace(filterTicket) ? true : r.LowPrice.Symbol.Contains(filterTicket))
            .OrderByDescending(x => x.Percent)
            .ToArray());
    }
}

public static class MathExtensions
{
    public static double PercentOf(this double value, double percent)
    {
        return value * (percent / 100);
    }
    public static decimal PercentOf(this decimal value, decimal percent)
    {
        return value * (percent / 100);
    }
}

