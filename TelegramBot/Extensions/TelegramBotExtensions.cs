using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using TelegramBot.Controllers;

namespace TelegramBot.Extensions;

public static partial class TelegramBotExtensions
{
    public static IMvcBuilder AddTelegramBotControllers(this IMvcBuilder mvcBuilder)
        => mvcBuilder.AddApplicationPart(typeof(TelegramController).Assembly);

    public static IHttpClientBuilder AddTelegramBot(this IServiceCollection services, IConfiguration configuration)
    {
        var botConfigSection = configuration.GetSection("BotConfiguration");
        services.Configure<BotConfiguration>(botConfigSection);

        services.AddScoped<TelegramService>();
        services.AddScoped<UpdateHandler>();
        services.ConfigureTelegramBotMvc();

        return services.AddHttpClient("tgwebhook")
            .RemoveAllLoggers()
            .AddTypedClient<ITelegramBotClient>(httpClient => new TelegramBotClient(botConfigSection.Get<BotConfiguration>()!.BotToken, httpClient));
    }
}