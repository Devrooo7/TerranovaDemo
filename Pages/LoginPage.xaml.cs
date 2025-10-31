using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using TerranovaDemo.Services;

namespace TerranovaDemo;

public partial class LoginPage : ContentPage
{
    public LoginPage()
    {
        InitializeComponent();
    }

    private async void LoginBtn_Clicked(object sender, EventArgs e)
    {
        try
        {
            StatusLbl.Text = "Autenticando...";
            LoginBtn.IsEnabled = false;

            var email = EmailEntry?.Text?.Trim() ?? "";
            var pass = PasswordEntry?.Text ?? "";

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
            {
                await DisplayAlert("Error", "Ingresa correo y contraseña.", "OK");
                return;
            }

            var success = await AuthService.LoginUser(email, pass);
            if (!success)
            {
                await DisplayAlert("Error", "Credenciales inválidas.", "OK");
                return;
            }

            // Guardar en Preferences
            Preferences.Set("UserId", AppState.CurrentUserUid);
            Preferences.Set("UserName", AppState.CurrentUserName);

            // Cambiar la página raíz de forma segura
            if (Application.Current.Windows.Count > 0)
                Application.Current.Windows[0].Page = new AppShell();
            else
                Application.Current.MainPage = new AppShell();
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            LoginBtn.IsEnabled = true;
            StatusLbl.Text = "";
        }
    }

    private async void GoRegister_Clicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new RegisterPage());
    }
}
