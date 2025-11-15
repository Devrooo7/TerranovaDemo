using Microsoft.Maui.Controls;
using TerranovaDemo.Services;

namespace TerranovaDemo
{
    public partial class LoginPage : ContentPage
    {
        private readonly AuthService _auth;

        // Constructor requerido por DI
        public LoginPage(AuthService auth)
        {
            InitializeComponent();
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        }

        // Constructor vacío opcional para XAML: resuelve desde DI
        public LoginPage() : this(ResolveAuthService()) { }

        private static AuthService ResolveAuthService()
        {
            var auth = IPlatformApplication.Current.Services.GetService<AuthService>();
            if (auth == null)
                throw new InvalidOperationException("AuthService no está registrado en DI. Revisa MauiProgram.cs.");
            return auth;
        }

        private async void LoginBtn_Clicked(object sender, EventArgs e)
        {
            try
            {
                StatusLbl.Text = "Autenticando...";
                LoginBtn.IsEnabled = false;

                string email = EmailEntry?.Text?.Trim() ?? "";
                string pass = PasswordEntry?.Text ?? "";

                if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
                {
                    await DisplayAlert("Error", "Ingresa correo y contraseña.", "OK");
                    return;
                }

                bool success = await _auth.LoginAsync(email, pass);

                if (!success)
                {
                    await DisplayAlert("Error", "Correo o contraseña incorrectos.", "OK");
                    return;
                }

                Application.Current.MainPage = new AppShell();
                await Shell.Current.GoToAsync("//mainpage");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                StatusLbl.Text = "";
                LoginBtn.IsEnabled = true;
            }
        }

        private async void GoRegister_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new RegisterPage());
        }
    }
}
