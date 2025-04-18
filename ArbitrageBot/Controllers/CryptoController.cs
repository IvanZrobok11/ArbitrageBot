using BusinessLogic.Extensions;
using BusinessLogic.Services;
using Microsoft.AspNetCore.Mvc;

namespace ArbitrageBot.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/crypto")]
[ApiVersion("1.0")]
public class CryptoV1Controller(CommonExchangeService commonExchangeService) : ControllerBase
{
    [HttpGet("prices")]
    public async Task<IActionResult> GetAssets(
        [FromQuery] ushort minPercent,
        [FromQuery] ushort maxPercent,
        [FromQuery] string? filterTicket,
        [FromQuery] bool matchNetworks, CancellationToken cancellationToken)
    {
        var results = await commonExchangeService.GetAssetsPairsAsync(minPercent, maxPercent, matchNetworks, cancellationToken).ToListAsync(cancellationToken);
        //results.ForEach(x => x.Percent = Math.Round(r.Percent, 3, MidpointRounding.ToEven))
        return Ok(results
            .Select(r => new { r.LowPriceAsset, r.BigPriceAsset, Percent = r.DiffPricePercent.RoundDecimals(3) })
            .Where(r => string.IsNullOrWhiteSpace(filterTicket) ? true : r.LowPriceAsset.Symbol.Contains(filterTicket))
            .OrderByDescending(x => x.Percent)
            .ToArray());
    }
}

[ApiController]
[Route("api/v{version:apiVersion}/crypto")]
[ApiVersion("2.0")]
public class CryptoV2Controller(CommonExchangeService commonExchangeService) : ControllerBase
{
    [HttpGet("prices")]
    public async Task<IActionResult> GetAssets(
        [FromQuery] ushort minPercent,
        [FromQuery] ushort maxPercent,
        [FromQuery] string? filterTicket,
        [FromQuery] bool matchNetworks, CancellationToken cancellationToken)
    {
        var results = await commonExchangeService
            .GetSmartAssetPairsAsync(minPercent, maxPercent, cancellationToken);

        return Ok(results
            .Where(r => string.IsNullOrWhiteSpace(filterTicket) ? true : r.Symbol.Contains(filterTicket))
            .OrderByDescending(x => x.DiffPercent)
            .ToArray());
    }
}
