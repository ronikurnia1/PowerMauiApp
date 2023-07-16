﻿// ----------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.
// ----------------------------------------------------------------------------

namespace PowerMauiApp.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.PowerBI.Api;
using Microsoft.PowerBI.Api.Models;
using Microsoft.Rest;
using PowerMauiApp.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.InteropServices;

public class PbiEmbedService
{
    private readonly PowerBI powerBI;
    private readonly AadService aadService;
    private readonly string powerBiApiUrl = "https://api.powerbi.com";

    public PbiEmbedService(AadService aadService, IConfiguration config)
    {
        this.aadService = aadService;
        powerBI = new PowerBI();
        config.GetSection("PowerBI").Bind(powerBI);
    }

    /// <summary>
    /// Get Power BI client
    /// </summary>
    /// <returns>Power BI client object</returns>
    public async Task<PowerBIClient> GetPowerBIClient()
    {
        var token = await aadService.GetAccessToken();
        var tokenCredentials = new TokenCredentials(token, "Bearer");
        return new PowerBIClient(new Uri(powerBiApiUrl), tokenCredentials);
    }

    /// <summary>
    /// Get embed params for a report
    /// </summary>
    /// <returns>Wrapper object containing Embed token, Embed URL, Report Id, and Report name for single report</returns>
    public async Task<EmbedParams> GetEmbedParams(Guid workspaceId, Guid reportId, string userName, [Optional] Guid additionalDatasetId)
    {
        PowerBIClient pbiClient = await GetPowerBIClient();

        // Get report info
        //var pbiReport = await pbiClient.Reports.GetReportInGroupAsync(workspaceId, reportId);
        var pbiReport = await pbiClient.Reports.GetReportAsync(workspaceId, reportId);

        //  Check if dataset is present for the corresponding report
        //  If isRDLReport is true then it is a RDL Report 
        var isRDLReport = string.IsNullOrEmpty(pbiReport.DatasetId);

        EmbedToken embedToken;

        // Generate embed token for RDL report if dataset is not present
        if (isRDLReport)
        {
            // Get Embed token for RDL Report
            embedToken = await GetEmbedTokenForRDLReport(workspaceId, reportId);
        }
        else
        {
            // Create list of datasets
            var datasetIds = new List<Guid>
            {
                // Add dataset associated to the report
                Guid.Parse(pbiReport.DatasetId)
            };

            // Append additional dataset to the list to achieve dynamic binding later
            if (additionalDatasetId != Guid.Empty)
            {
                datasetIds.Add(additionalDatasetId);
            }

            // Get Embed token multiple resources
            embedToken = await GetEmbedToken(reportId, datasetIds, userName, workspaceId);
        }

        // Add report data for embedding
        var embedReports = new List<EmbedReport>() {
            new EmbedReport
            {
                ReportId = pbiReport.Id, ReportName = pbiReport.Name, EmbedUrl = pbiReport.EmbedUrl
            }
        };

        // Capture embed params
        var embedParams = new EmbedParams
        {
            EmbedReport = embedReports,
            Type = "Report",
            EmbedToken = embedToken
        };

        return embedParams;
    }

    /// <summary>
    /// Get embed params for multiple reports for a single workspace
    /// </summary>
    /// <returns>Wrapper object containing Embed token, Embed URL, Report Id, and Report name for multiple reports</returns>
    /// <remarks>This function is not supported for RDL Report</remakrs>
    public async Task<EmbedParams> GetEmbedParams(Guid workspaceId, IList<Guid> reportIds, [Optional] IList<Guid> additionalDatasetIds)
    {
        // Note: This method is an example and is not consumed in this sample app

        PowerBIClient pbiClient = await GetPowerBIClient();

        // Create mapping for reports and Embed URLs
        var embedReports = new List<EmbedReport>();

        // Create list of datasets
        var datasetIds = new List<Guid>();

        // Get datasets and Embed URLs for all the reports
        foreach (var reportId in reportIds)
        {
            // Get report info
            var pbiReport = pbiClient.Reports.GetReportInGroup(workspaceId, reportId);

            datasetIds.Add(Guid.Parse(pbiReport.DatasetId));

            // Add report data for embedding
            embedReports.Add(new EmbedReport { ReportId = pbiReport.Id, ReportName = pbiReport.Name, EmbedUrl = pbiReport.EmbedUrl });
        }

        // Append to existing list of datasets to achieve dynamic binding later
        if (additionalDatasetIds != null)
        {
            datasetIds.AddRange(additionalDatasetIds);
        }

        // Get Embed token multiple resources
        var embedToken = await GetEmbedToken(reportIds, datasetIds, workspaceId);

        // Capture embed params
        var embedParams = new EmbedParams
        {
            EmbedReport = embedReports,
            Type = "Report",
            EmbedToken = embedToken
        };

        return embedParams;
    }

    /// <summary>
    /// Get Embed token for single report, multiple datasets, and an optional target workspace
    /// </summary>
    /// <returns>Embed token</returns>
    /// <remarks>This function is not supported for RDL Report</remakrs>
    public async Task<EmbedToken> GetEmbedToken(Guid reportId, IList<Guid> datasetIds, string userName, [Optional] Guid targetWorkspaceId)
    {
        PowerBIClient pbiClient = await GetPowerBIClient();

        // Defines the user identity and roles.
        var rlsIdentity = new EffectiveIdentity(
            username: userName,
            roles: new List<string> { "Role CurrUser" },
            datasets: datasetIds.Select(i => i.ToString()).ToList()
        );


        // Create a request for getting Embed token 
        // This method works only with new Power BI V2 workspace experience
        var tokenRequest = new GenerateTokenRequestV2(
            reports: new List<GenerateTokenRequestV2Report>() { new GenerateTokenRequestV2Report(reportId) },
            datasets: datasetIds.Select(datasetId => new GenerateTokenRequestV2Dataset(datasetId.ToString())).ToList(),
            targetWorkspaces: targetWorkspaceId != Guid.Empty ? new List<GenerateTokenRequestV2TargetWorkspace>() { new GenerateTokenRequestV2TargetWorkspace(targetWorkspaceId) } : null,
            identities: new List<EffectiveIdentity> { rlsIdentity }
        );

        // Generate Embed token
        var embedToken = pbiClient.EmbedToken.GenerateToken(tokenRequest);

        return embedToken;
    }

    /// <summary>
    /// Get Embed token for multiple reports, datasets, and an optional target workspace
    /// </summary>
    /// <returns>Embed token</returns>
    /// <remarks>This function is not supported for RDL Report</remakrs>
    public async Task<EmbedToken> GetEmbedToken(IList<Guid> reportIds, IList<Guid> datasetIds, [Optional] Guid targetWorkspaceId)
    {
        // Note: This method is an example and is not consumed in this sample app

        PowerBIClient pbiClient = await GetPowerBIClient();

        // Convert report Ids to required types
        var reports = reportIds.Select(reportId => new GenerateTokenRequestV2Report(reportId)).ToList();

        // Convert dataset Ids to required types
        var datasets = datasetIds.Select(datasetId => new GenerateTokenRequestV2Dataset(datasetId.ToString())).ToList();

        // Create a request for getting Embed token 
        // This method works only with new Power BI V2 workspace experience
        var tokenRequest = new GenerateTokenRequestV2(

            datasets: datasets,

            reports: reports,

            targetWorkspaces: targetWorkspaceId != Guid.Empty ? new List<GenerateTokenRequestV2TargetWorkspace>() { new GenerateTokenRequestV2TargetWorkspace(targetWorkspaceId) } : null
        );

        // Generate Embed token
        var embedToken = pbiClient.EmbedToken.GenerateToken(tokenRequest);

        return embedToken;
    }

    /// <summary>
    /// Get Embed token for multiple reports, datasets, and optional target workspaces
    /// </summary>
    /// <returns>Embed token</returns>
    /// <remarks>This function is not supported for RDL Report</remakrs>
    public async Task<EmbedToken> GetEmbedToken(IList<Guid> reportIds, IList<Guid> datasetIds, [Optional] IList<Guid> targetWorkspaceIds)
    {
        // Note: This method is an example and is not consumed in this sample app

        PowerBIClient pbiClient = await GetPowerBIClient();

        // Convert report Ids to required types
        var reports = reportIds.Select(reportId => new GenerateTokenRequestV2Report(reportId)).ToList();

        // Convert dataset Ids to required types
        var datasets = datasetIds.Select(datasetId => new GenerateTokenRequestV2Dataset(datasetId.ToString())).ToList();

        // Convert target workspace Ids to required types
        IList<GenerateTokenRequestV2TargetWorkspace> targetWorkspaces = null;
        if (targetWorkspaceIds != null)
        {
            targetWorkspaces = targetWorkspaceIds.Select(targetWorkspaceId => new GenerateTokenRequestV2TargetWorkspace(targetWorkspaceId)).ToList();
        }

        // Create a request for getting Embed token 
        // This method works only with new Power BI V2 workspace experience
        var tokenRequest = new GenerateTokenRequestV2(

            datasets: datasets,

            reports: reports,

            targetWorkspaces: targetWorkspaceIds != null ? targetWorkspaces : null
        );

        // Generate Embed token
        var embedToken = pbiClient.EmbedToken.GenerateToken(tokenRequest);

        return embedToken;
    }

    /// <summary>
    /// Get Embed token for RDL Report
    /// </summary>
    /// <returns>Embed token</returns>
    public async Task<EmbedToken> GetEmbedTokenForRDLReport(Guid targetWorkspaceId, Guid reportId, string accessLevel = "view")
    {
        PowerBIClient pbiClient = await GetPowerBIClient();

        // Generate token request for RDL Report
        var generateTokenRequestParameters = new GenerateTokenRequest(
            accessLevel: accessLevel
        );

        // Generate Embed token
        var embedToken = pbiClient.Reports.GenerateTokenInGroup(targetWorkspaceId, reportId, generateTokenRequestParameters);

        return embedToken;
    }
}
