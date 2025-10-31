using TerranovaDemo.Services;
using Microsoft.Maui.Controls;
using System.Net.Http;
using System.Threading.Tasks;

namespace TerranovaDemo
{
    public partial class SettingsPage : ContentPage
    {
        public static string SavedESP32Ip { get; set; } = "";
        public static string SavedPhoneNumber { get; set; } = "";

        private const string DEFAULT_ESP32_IP = "192.168.1.16";

        public SettingsPage()
        {
            InitializeComponent();
        }

        private void SavePhoneButton_Clicked(object sender, EventArgs e)
        {
            SavedESP32Ip = Esp32IpEntry.Text?.Trim() ?? string.Empty;
            SavedPhoneNumber = PhoneNumberEntry.Text?.Trim() ?? string.Empty;

            DisplayAlert("✅ Guardado", "Configuraciones almacenadas correctamente.", "OK");
        }

        private async void LogoutButton_Clicked(object sender, EventArgs e)
        {
            await AuthService.LogoutAsync();
            await DisplayAlert("Cierre de sesión", "Has cerrado sesión correctamente.", "OK");
            Application.Current.Windows[0].Page = new NavigationPage(new LoginPage());
        }

        private async void ConnectESP32_Clicked(object sender, EventArgs e)
        {
            string ipToUse = !string.IsNullOrWhiteSpace(Esp32IpEntry.Text)
                             ? Esp32IpEntry.Text.Trim()
                             : DEFAULT_ESP32_IP;

            bool connected = await ConnectESP32Automatically(ipToUse);
            ConnectionStatus.Text = connected ? $"Estado: Conectado ✅" : $"Estado: Desconectado ❌";
            ConnectionStatus.TextColor = connected ? Colors.Green : Colors.Red;

            await DisplayAlert("Conexión ESP32",
                               connected ? $"Conectado a {ipToUse} ✅" : $"No se pudo conectar a {ipToUse} ❌",
                               "OK");
        }

        public static async Task<bool> ConnectESP32Automatically(string ip)
        {
            try
            {
                SavedESP32Ip = ip;

                using var client = new HttpClient();
                client.Timeout = System.TimeSpan.FromSeconds(2);
                var response = await client.GetAsync($"http://{ip}/ping");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private async void DeleteAccountButton_Clicked(object sender, EventArgs e)
        {
            bool confirm1 = await DisplayAlert("Eliminar cuenta", "¿Estás seguro de eliminar tu cuenta?", "Sí", "No");
            if (!confirm1) return;

            bool confirm2 = await DisplayAlert("Confirmar eliminación", "Esta acción no se puede revertir. ¿Deseas continuar?", "Sí", "No");
            if (confirm2)
            {
                await DisplayAlert("Cuenta eliminada", "Tu cuenta ha sido eliminada.", "OK");
                Application.Current.Windows[0].Page = new NavigationPage(new LoginPage());
            }
        }
    }
}
