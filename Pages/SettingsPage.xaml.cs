using TerranovaDemo.Services;
using Microsoft.Maui.Controls;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using System.Net.NetworkInformation;

namespace TerranovaDemo
{
    public partial class SettingsPage : ContentPage
    {
        public static string SavedESP32Host { get; set; } = "";
        public static string SavedPhoneNumber { get; set; } = "";

        private const string DEFAULT_MDNS = "terranova.local";
        private const int UDP_PORT = 4210; // Puerto de descubrimiento
        private const int ESP_HTTP_PORT = 80;
        private const int DISCOVERY_TIMEOUT = 3000; // 3s

        public SettingsPage()
        {
            InitializeComponent();
            ConnectToESP32Automatically();
        }

        // 🔄 Intento de conexión automática
        private async void ConnectToESP32Automatically()
        {
            ConnectionStatus.Text = "Buscando ESP32...";
            ConnectionStatus.TextColor = Colors.Orange;

            bool connected = await DiscoverAndConnectESP32();

            ConnectionStatus.Text = connected
                ? $"Estado: Conectado ✅ ({SavedESP32Host})"
                : "Estado: Desconectado ❌";
            ConnectionStatus.TextColor = connected ? Colors.Green : Colors.Red;

            if (connected)
                await DisplayAlert("Conexión exitosa", $"ESP32 detectado en {SavedESP32Host} ✅", "OK");
            else
                await DisplayAlert("Error", "No se pudo detectar el ESP32 automáticamente. Verifica que ambos estén en la misma red WiFi.", "OK");
        }

        // 🔍 Descubre el ESP32 por broadcast UDP (solo Android/iOS)
        private async Task<bool> DiscoverAndConnectESP32()
        {
#if WINDOWS
            await DisplayAlert("Aviso", "El escaneo local no está disponible en Windows por restricciones de red.", "OK");
            return await TryConnectHttp(DEFAULT_MDNS);
#else
            try
            {
                using var udp = new UdpClient();
                udp.EnableBroadcast = true;
                udp.Client.ReceiveTimeout = DISCOVERY_TIMEOUT;

                byte[] message = Encoding.ASCII.GetBytes("DISCOVER_TERRANOVA");
                var broadcastEP = new IPEndPoint(IPAddress.Broadcast, UDP_PORT);
                await udp.SendAsync(message, message.Length, broadcastEP);

                var receiveTask = udp.ReceiveAsync();
                var completed = await Task.WhenAny(receiveTask, Task.Delay(DISCOVERY_TIMEOUT));

                if (completed == receiveTask)
                {
                    var response = receiveTask.Result;
                    string reply = Encoding.ASCII.GetString(response.Buffer);
                    if (reply.StartsWith("TERRANOVA_OK"))
                    {
                        string espIP = response.RemoteEndPoint.Address.ToString();
                        if (await TryConnectHttp(espIP))
                        {
                            SavedESP32Host = espIP;
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error descubrimiento: " + ex.Message);
            }

            return await TryConnectHttp(DEFAULT_MDNS);
#endif
        }

        // 🌐 Prueba conexión HTTP /ping
        private static async Task<bool> TryConnectHttp(string host)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };
                var response = await client.GetAsync($"http://{host}:{ESP_HTTP_PORT}/ping");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        // 🔁 Reconexión manual
        private async void ConnectESP32_Clicked(object sender, EventArgs e)
        {
            ConnectionStatus.Text = "Reconectando ESP32...";
            ConnectionStatus.TextColor = Colors.Orange;

            bool connected = await DiscoverAndConnectESP32();

            ConnectionStatus.Text = connected
                ? $"Estado: Conectado ✅ ({SavedESP32Host})"
                : "Estado: Desconectado ❌";
            ConnectionStatus.TextColor = connected ? Colors.Green : Colors.Red;

            await DisplayAlert("Conexión ESP32",
                connected ? $"Conectado a {SavedESP32Host} ✅" : "No se pudo conectar al ESP32 ❌",
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

            Application.Current.MainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#4CAF50"), // ✅ verde consistente
                BarTextColor = Colors.White
            };
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

                Application.Current.MainPage = new NavigationPage(new LoginPage())
                {
                    BarBackgroundColor = Color.FromArgb("#4CAF50"), // ✅ mantiene color verde
                    BarTextColor = Colors.White
                };
            }
        }
    }
}
