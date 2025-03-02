using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;
using System.Net;

namespace BusinessLogic.HttpClientPolicy;

public static class HttpClientExtension
{
    public static void AddHttpClientPolicy(this IHttpClientBuilder builder, IConfiguration configuration)
    {
        var clientPolicySettings = configuration.GetSection(HttpClientPolicy.SectionKey).Get<HttpClientPolicy>()!;

        builder.SetHandlerLifetime(TimeSpan.FromMinutes(clientPolicySettings.HandlerLifetime))  //Set lifetime to five minutes
            .AddPolicyHandler(GetRetryPolicy());

        IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(msg => msg.StatusCode == HttpStatusCode.NotFound)
                .WaitAndRetryAsync(clientPolicySettings.RetryCount,
                    retryAttempt => TimeSpan.FromMilliseconds(clientPolicySettings.RetryTimeout) * retryAttempt);
        }
    }
}

