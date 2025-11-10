using Microsoft.Maui.Controls;
using TerranovaDemo.Services;

namespace TerranovaDemo
{
    public partial class LoginPage : ContentPage
    {
        private double _lastWidth = -1;
        private double _lastHeight = -1;

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

                // ✅ Guardar sesión
                Preferences.Set("UserId", AppState.CurrentUserUid);
                Preferences.Set("UserName", AppState.CurrentUserName);

                // ✅ Cambiar a AppShell (con barra verde y menú hamburguesa)
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
            // ✅ Abre la página de registro dentro del mismo contexto
            await Navigation.PushAsync(new RegisterPage());
        }

        // ✅ Protección contra recursión por cambio de tamaño
        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);

            if (Math.Abs(width - _lastWidth) > 0.1 || Math.Abs(height - _lastHeight) > 0.1)
            {
                _lastWidth = width;
                _lastHeight = height;

                // Si quieres hacer ajustes visuales según tamaño, hazlo aquí.
                // No llames ForceLayout() para evitar recursión infinita.
            }
        }
    }
}
