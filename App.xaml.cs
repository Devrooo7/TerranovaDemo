namespace TerranovaDemo;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var nav = new NavigationPage(new LoginPage());
        return new Window(nav);
    }
}
