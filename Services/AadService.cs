// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
// ----------------------------------------------------------------------------

namespace PowerMauiApp.Services;

using Microsoft.Extensions.Configuration;
using PowerMauiApp.Models;
using System.Text.Json;

public class AadService
{
    private readonly AzureAd azureAd;
    private readonly HttpClient httpClient;
    public AadService(IConfiguration config, HttpClient httpClient)
    {
        azureAd = new AzureAd();
        config.GetSection("AzureAd").Bind(azureAd);
        this.httpClient = httpClient;
    }

    /// <summary>
    /// Generates and returns Access token
    /// </summary>
    /// <returns>AAD token</returns>
    public async Task<string> GetAccessToken()
    {
        var requestUrl = $"https://login.microsoftonline.com/{azureAd.TenantId}/oauth2/v2.0/token";
        var dict = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", azureAd.ClientId },
            { "client_secret", azureAd.ClientSecret },
            { "scope", azureAd.ScopeBase[0] }
        };

        var requestBody = new FormUrlEncodedContent(dict);
        var response = await httpClient.PostAsync(requestUrl, requestBody);

        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var aadToken = JsonSerializer.Deserialize<AzureADToken>(responseContent);
        return aadToken.AccessToken;
    }
}
