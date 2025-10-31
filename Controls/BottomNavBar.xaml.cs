using Microsoft.Maui.Controls;

namespace TerranovaDemo;

public partial class BottomNavBar : ContentView
{
    public BottomNavBar()
    {
        InitializeComponent();
        Shell.Current.Navigated += Shell_Navigated;
        UpdateActiveButton(Shell.Current.CurrentState.Location.ToString());
    }

    private void Shell_Navigated(object? sender, ShellNavigatedEventArgs e)
        => UpdateActiveButton(e.Current.Location.ToString());

    private void UpdateActiveButton(string location)
    {
        HomeButton.Scale = 1;
        UserButton.Scale = 1;
        SettingsButton.Scale = 1;

        if (location.Contains("mainpage", StringComparison.OrdinalIgnoreCase))
            HomeButton.Scale = 1.3;
        else if (location.Contains("user", StringComparison.OrdinalIgnoreCase))
            UserButton.Scale = 1.3;
        else if (location.Contains("settings", StringComparison.OrdinalIgnoreCase))
            SettingsButton.Scale = 1.3;
    }

    private async void HomeButton_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("///mainpage");
    private async void UserButton_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("///user");
    private async void SettingsButton_Clicked(object sender, EventArgs e) => await Shell.Current.GoToAsync("///settings");
}
