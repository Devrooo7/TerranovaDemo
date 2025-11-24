using Microsoft.Maui.Controls;
using TerranovaDemo.Services;

namespace TerranovaDemo
{
    public partial class RegisterPage : ContentPage
    {
        private readonly AuthService _auth;

        public RegisterPage(AuthService auth)
        {
            InitializeComponent();
            _auth = auth ?? throw new ArgumentNullException(nameof(auth));
        }

        public RegisterPage() : this(ResolveAuthService()) { }

        private static AuthService ResolveAuthService()
        {
            var mauiContext = App.Current?.Handler?.MauiContext;
            var auth = mauiContext?.Services.GetService<AuthService>();
            if (auth == null)
                throw new InvalidOperationException("AuthService no está registrado en DI. Revisa MauiProgram.cs.");
            return auth;
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
                var phone = PhoneEntry?.Text?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(pass))
                {
                    await DisplayAlert("Error", "Completa todos los campos.", "OK");
                    return;
                }

                var success = await _auth.RegisterUserAsync(email, pass, name, phone);
                if (!success)
                {
                    await DisplayAlert("Error", "No se pudo crear la cuenta.", "OK");
                    return;
                }

                await DisplayAlert("Registro exitoso", "Tu cuenta fue creada. Inicia sesión.", "OK");
                await Navigation.PopAsync();
            }
            finally
            {
                RegisterBtn.IsEnabled = true;
                StatusLbl.Text = string.Empty;
            }
        }

        private async void GoLogin_Clicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new LoginPage());
        }
    }
}
