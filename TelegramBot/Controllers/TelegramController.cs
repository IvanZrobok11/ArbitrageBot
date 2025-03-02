using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace TelegramBot.Controllers;

[ApiController]
[Route("[controller]")]
public class TelegramController(TelegramService telegramService, IOptions<BotConfiguration> Config, ITelegramBotClient bot, UpdateHandler handleUpdateService) : ControllerBase
{
    [HttpGet("webhookInfo")]
    public async Task<JsonResult> GetInfo()
    {
        var info = await bot.GetWebhookInfo();

        return new JsonResult(info);
    }

    [HttpGet("setWebhook")]
    public async Task<string> SetWebHook(CancellationToken ct)
    {
        var webhookUrl = Config.Value.BotWebhookUrl.AbsoluteUri;
        await bot.SetWebhook(webhookUrl, allowedUpdates: [], secretToken: Config.Value.SecretToken, cancellationToken: ct);
        return $"Webhook set to {webhookUrl}";
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] Update update, CancellationToken ct)
    {
        if (Request.Headers["X-Telegram-Bot-Api-Secret-Token"] != Config.Value.SecretToken)
            return Forbid();
        try
        {
            await handleUpdateService.HandleUpdateAsync(bot, update, ct);
        }
        catch (Exception exception)
        {
            await handleUpdateService.HandleErrorAsync(bot, exception, Telegram.Bot.Polling.HandleErrorSource.HandleUpdateError, ct);
        }
        return Ok();
    }

    //[Authorize]
    //[HttpPost("set-configuration")]
    //public async Task<IActionResult> SetConfiguration([FromBody] string config)
    //{
    //    var userId = long.Parse(User.Identity.Name);
    //    await telegramService.SaveConfigurationAsync(userId, config);
    //    return Ok("Configuration saved successfully!");
    //}
}
