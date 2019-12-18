using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Shouldly;

namespace MhLabs.AwsSignedHttpClient.Tests
{

    public class GoogleService : IGoogleService
    {
        private readonly HttpClient _client;
        private readonly ILogger<GoogleService> _logger;

        public GoogleService(HttpClient httpClient, ILogger<GoogleService> logger)
        {
            _client = httpClient;
            _logger = logger;
        }

        public async Task Get(string query, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Search Google: {Query}", query);
            await _client.SendAsync<string>(HttpMethod.Get, $"/complete/search?q={query}", cancellationToken: cancellationToken);
        }
    }
}