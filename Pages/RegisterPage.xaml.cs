using Microsoft.Maui.Controls;
using TerranovaDemo.Services;

namespace TerranovaDemo;

public partial class RegisterPage : ContentPage
{
    public RegisterPage()
    {
        InitializeComponent();
    }

    private async void RegisterBtn_Clicked(object sender, EventArgs e)
    {
        try
        {
            StatusLbl.Text = "Creando cuenta...";
            RegisterBtn.IsEnabled = false;

            var name = NameEntry?.Text?.Trim() ?? string.Empty;
            var email = EmailEntry?.Text?.Trim() ?? string.Empty;
            var pass = PasswordEntry?.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
            {
                await DisplayAlert("Error", "Completa todos los campos.", "OK");
                return;
            }

            var success = await AuthService.RegisterUser(email, pass, name);
            if (!success)
            {
                await DisplayAlert("Error", "No se pudo crear la cuenta.", "OK");
                return;
            }

            Preferences.Set("UserId", AppState.CurrentUserUid);
            Preferences.Set("UserName", AppState.CurrentUserName);

            await DisplayAlert("Registro exitoso", "Tu cuenta fue creada. Inicia sesión.", "OK");

            if (Navigation.NavigationStack.Count > 0)
                await Navigation.PopAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            RegisterBtn.IsEnabled = true;
            StatusLbl.Text = string.Empty;
        }
    }
}
