using Confluent.Kafka;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Net.Http;
using System.Security.Authentication;
using HealthChecks.Providers;

namespace HealthChecks
{
    public static class HealthChecksConfig
    {
        public static IServiceCollection AddHealthChecksConfigConfiguration(this IServiceCollection services)
        {
            services.AddHealthChecks()
                .AddCheck("ping", new PingHealthCheck(), HealthStatus.Healthy, tags: new string[] { "ping" })
                .AddSqlServer(EnvironmentVariableProvider.Instance.Get(EnvironmentVariablesConstants.SQLSERVER_CONNECTION_STRING_HANGFIRE),
                    name: "SqlServer-Hangfire", tags: new string[] { "db", "data" })
                .AddSqlServer(EnvironmentVariableProvider.Instance.Get(EnvironmentVariablesConstants.SQLSERVER_CONNECTION_STRING),
                    name: "SqlServer", tags: new string[] { "db", "data" })
                .AddSqlServer(EnvironmentVariableProvider.Instance.Get(EnvironmentVariablesConstants.SQLSERVER_CONNECTION_STRING),
                    name: "SqlServer", tags: new string[] { "db", "data" })
                .AddSqlServer(EnvironmentVariableProvider.Instance.Get(EnvironmentVariablesConstants.SQLSERVER_CONNECTION_STRING),
                    name: "SqlServer", tags: new string[] { "db", "data" })
                .AddKafka(new ProducerConfig()
                {
                    BootstrapServers = EnvironmentVariableProvider.Instance.Get(EnvironmentVariablesConstants.KAFKA_BOOTSTRAP_SERVERS)
                },  name: "Kafka", tags: new string[] { "message", "data" }, timeout: new TimeSpan(0, 0, 5))
                .AddCheck("HttpClient-Proxy", new TypedHttpClientHealthCheck(services, HttpMethod.Options, nameof(IProxyClient)),
                    tags: new string[] { "http" })
                .AddCheck("HttpClient-Linx", new TypedHttpClientHealthCheck(services, HttpMethod.Get, nameof(IPixClient), ObterLinxUrn()),
                    tags: new string[] { "http" });



            services
                .AddHealthChecksUI(settings => settings.AddHealthCheckEndpoint("API PIX", ObterEndpointHealthcheck())
                    .SetEvaluationTimeInSeconds((int)TimeSpan.FromMinutes(2).TotalSeconds)
                    .UseApiEndpointHttpMessageHandler(sp => new HttpClientHandler
                    {
                        ClientCertificateOptions = ClientCertificateOption.Manual,
                        SslProtocols = SslProtocols.Tls12,
                        ServerCertificateCustomValidationCallback = delegate { return true; }
                    }))
                .AddInMemoryStorage();

            return services;
        }

        public static IEndpointRouteBuilder ConfigureHealthcheck(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            endpoints.MapHealthChecks("/hc/ping", new HealthCheckOptions()
            {
                Predicate = check => check.Tags.Contains("ping"),
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });
            endpoints.MapHealthChecksUI(setup =>
            {
                setup.ResourcesPath = "/healthchecks-ui/resources";
                setup.ApiPath = "/healthchecks-ui/api";
            });
            return endpoints;
        }

        private static string ObterEndpointHealthcheck()
        {
            var urlExterna = EnvironmentVariableProvider.Instance.Get(EnvironmentVariablesConstants.URL_API_PIX);
            if (!string.IsNullOrEmpty(urlExterna))
                return $"{urlExterna.TrimEnd('/')}/hc";
            return "/hc";
        }

        private static string ObterLinxUrn()
        {
            return $"{EnvironmentVariableProvider.Instance.Get(EnvironmentVariablesConstants.LINX_URN_CONSULTAR_PAGAMENTO)}"+
                   $"{EnvironmentVariableProvider.Instance.Get(EnvironmentVariablesConstants.HEALTHCHECK_LINX_PAGAMENTO_ID)}";
        }
    }
}
