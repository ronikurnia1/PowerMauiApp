export function showReport(accessToken, embedUrl, embedReportId) {
    // Get models. models contains enums that can be used.
    var reportContainer = document.getElementById("report-container");
    var models = window['powerbi-client'].models;
    var config = {
        type: 'report',
        tokenType: models.TokenType.Embed,
        accessToken: accessToken,
        embedUrl: embedUrl,
        id: embedReportId,
        permissions: models.Permissions.All,
        settings: {
            filterPaneEnabled: true,
            navContentPaneEnabled: true,
            layoutType: models.LayoutType.MobilePortrait,
            //customLayout: {
            //    displayOption: models.DisplayOption.FitToPage
            //},
            pageView: 'oneColumn',
            panes: {
                filters: {
                    visible: false
                }
            }
        }
    };
    // Embed the report and display it within the div container.
    powerbi.embed(reportContainer, config);
}
