using Microsoft.AspNetCore.Components;

namespace PowerMauiApp;

public partial class AppShell : Shell
{
    private readonly RouterData routerData;
    private readonly NavigationManager navigationManager;

    public AppShell(RouterData routerData, NavigationManager navigationManager)
    {
        this.routerData = routerData;
        this.navigationManager = navigationManager;
        InitializeComponent();
    }

    private void DataTab_Appearing(object sender, EventArgs e)
    {
        routerData.IsReport = false;
    }

    private void ReportTab_Appearing(object sender, EventArgs e)
    {
        routerData.IsReport = true;
    }

}