using Microsoft.AspNetCore.Components;

namespace PowerMauiApp;

public partial class App : Application
{
    public App(RouterData routerData, NavigationManager navigationManager)
    {
        InitializeComponent();
        MainPage = new AppShell(routerData, navigationManager);
    }
}