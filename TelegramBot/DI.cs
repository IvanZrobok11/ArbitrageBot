using BusinessLogic.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TelegramBot.Controllers;

namespace TelegramBot;

public static partial class DI
{
    public static IMvcBuilder AddTelegramBotControllers(this IMvcBuilder mvcBuilder)
        => mvcBuilder.AddApplicationPart(typeof(TelegramController).Assembly);

    public static void AddTelegramBot(this IServiceCollection services, IConfiguration configuration)
    {
        var botConfigSection = configuration.GetSection("BotConfiguration");
        services.Configure<BotConfiguration>(botConfigSection);

        services.AddScoped<UpdateHandler>();
        services.AddScoped<TelegramAssetsSender>();
        services.ConfigureTelegramBotMvc();

        services.AddHttpClient("tgwebhook")
            .RemoveAllLoggers()
            .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botConfigSection.Get<BotConfiguration>()!.BotToken, httpClient))
            .AddHttpClientPolicy(configuration);
    }
}