using TerranovaDemo.Services;
using Microsoft.Maui.Controls;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TerranovaDemo
{
    public partial class SettingsPage : ContentPage
    {
        public static string SavedESP32Host { get; set; } = "";
        public static string SavedPhoneNumber { get; set; } = "";

        private const string DEFAULT_ESP32_HOST = "esp32sensor.local"; // 👈 Nombre mDNS del ESP32

        public SettingsPage()
        {
            InitializeComponent();
            ConnectToESP32Automatically(); // 🔄 Conexión automática al iniciar
        }

        // 🔄 Conecta automáticamente al ESP32 en la red local
        private async void ConnectToESP32Automatically()
        {
            ConnectionStatus.Text = "Buscando ESP32...";
            ConnectionStatus.TextColor = Colors.Orange;

            bool connected = await ConnectESP32Automatically(DEFAULT_ESP32_HOST);

            ConnectionStatus.Text = connected ? "Estado: Conectado ✅" : "Estado: Desconectado ❌";
            ConnectionStatus.TextColor = connected ? Colors.Green : Colors.Red;

            if (connected)
                await DisplayAlert("Conexión exitosa", $"ESP32 detectado automáticamente ({DEFAULT_ESP32_HOST}) ✅", "OK");
            else
                await DisplayAlert("Error", "No se pudo detectar el ESP32 automáticamente. Verifica que esté en la misma red WiFi.", "OK");
        }

        // 🔌 Método que intenta conectar al ESP32
        public static async Task<bool> ConnectESP32Automatically(string host)
        {
            try
            {
                SavedESP32Host = host;

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(3);
                var response = await client.GetAsync($"http://{host}/ping");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // 🔁 Reconectar manualmente
        private async void ConnectESP32_Clicked(object sender, EventArgs e)
        {
            await ConnectToESP32Manually();
        }

        private async Task ConnectToESP32Manually()
        {
            ConnectionStatus.Text = "Reconectando ESP32...";
            ConnectionStatus.TextColor = Colors.Orange;

            bool connected = await ConnectESP32Automatically(DEFAULT_ESP32_HOST);

            ConnectionStatus.Text = connected ? "Estado: Conectado ✅" : "Estado: Desconectado ❌";
            ConnectionStatus.TextColor = connected ? Colors.Green : Colors.Red;

            await DisplayAlert("Conexión ESP32",
                               connected ? $"Conectado a {DEFAULT_ESP32_HOST} ✅" : $"No se pudo conectar al ESP32 ❌",
                               "OK");
        }

        // 💾 Guardar número telefónico
        private void SavePhoneButton_Clicked(object sender, EventArgs e)
        {
            SavedPhoneNumber = PhoneNumberEntry.Text?.Trim() ?? string.Empty;
            DisplayAlert("✅ Guardado", "Configuraciones almacenadas correctamente.", "OK");
        }

        // 🚪 Cerrar sesión
        private async void LogoutButton_Clicked(object sender, EventArgs e)
        {
            await AuthService.LogoutAsync();
            await DisplayAlert("Cierre de sesión", "Has cerrado sesión correctamente.", "OK");
            Application.Current.Windows[0].Page = new NavigationPage(new LoginPage());
        }

        // 🗑️ Eliminar cuenta
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
