using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace HealthChecks.Providers
{
    public class TypedHttpClientHealthCheck : IHealthCheck
    {
        private readonly string _urn;
        private readonly Lazy<IServiceProvider> _serviceProvider;
        private readonly string _clientName;
        private readonly HttpMethod _httpMethod;

        public TypedHttpClientHealthCheck(IServiceCollection services, HttpMethod httpMethod, string clientName, string urn = null)
        {
            _serviceProvider = new Lazy<IServiceProvider>(services.BuildServiceProvider);
            _httpMethod = httpMethod;
            _clientName = clientName;
            _urn = urn;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                using var scope = _serviceProvider.Value.CreateScope();

                var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var httpClient = factory.CreateClient(_clientName);

                HttpRequestMessage request = new(_httpMethod, _urn);
                HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);

                Dictionary<string, object> data = new()
                {
                    { "url", response.RequestMessage.RequestUri.ToString() },
                    { "status", (int)response.StatusCode },
                    { "reason", response.ReasonPhrase }
                };

                if (response.IsSuccessStatusCode)
                    return HealthCheckResult.Healthy("A api está íntegra!", data);

                return HealthCheckResult.Unhealthy($"A api não está íntegra!", null, data);
            }
            catch (Exception ex)
            {
                Dictionary<string, object> data = new()
                {
                    { "clientName", _clientName },
                    { "clientUrn", _urn },
                    { "httpMethod", _httpMethod }
                };

                return HealthCheckResult.Unhealthy("Ocorreu uma exceção durante a verificação de integridade.", ex, data);
            }
        }
    }
}
