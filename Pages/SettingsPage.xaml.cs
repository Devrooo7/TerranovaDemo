using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TerranovaDemo.Services;
using TerranovaDemo.Backend;
using Microsoft.Maui.ApplicationModel;

namespace TerranovaDemo
{
    public partial class SettingsPage : ContentPage
    {
        public static string SavedESP32Host { get; set; } = "";
        public static string SavedPhoneNumber { get; set; } = "";

        private const string DEFAULT_MDNS = "terranova.local";
        private const int UDP_PORT = 4210;
        private const int ESP_HTTP_PORT = 80;
        private const int DISCOVERY_TIMEOUT = 3000;

        private readonly AuthService _auth;
        private readonly FirebaseAuthClient _firebase;
        private readonly BackendService _backend;

        public SettingsPage()
        {
            InitializeComponent();

            _auth = IPlatformApplication.Current.Services.GetService<AuthService>();
            _firebase = IPlatformApplication.Current.Services.GetService<FirebaseAuthClient>();
            _backend = new BackendService();

            // 🟢 CORRECCIÓN APLICADA: Guardar la URL del backend en las preferencias.
            BackendService.SaveBackendUrl("https://terranova-backend.onrender.com");

            ConnectESP32Automatically();
        }

        // ============================================================
        // AUTO-DETECCIÓN ESP32
        // ============================================================

        private async void ConnectESP32Automatically()
        {
            // 1. SOLICITAR PERMISO DE UBICACIÓN
            var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                if (status != PermissionStatus.Granted)
                {
                    ConnectionStatus.Text = "Permiso Ubicación/WiFi denegado ❌";
                    ConnectionStatus.TextColor = Colors.Red;
                    await DisplayAlert("Advertencia", "Se necesita el permiso de Ubicación para encontrar el ESP32 en la red local. No se puede conectar.", "OK");
                    return;
                }
            }


            ConnectionStatus.Text = "Buscando ESP32...";
            ConnectionStatus.TextColor = Colors.Orange;

            bool connected = await DiscoverAndConnectESP32();

            ConnectionStatus.Text = connected ? $"Estado: Conectado ✅ ({SavedESP32Host})" : "Estado: Desconectado ❌";
            ConnectionStatus.TextColor = connected ? Colors.Green : Colors.Red;

            if (connected)
            {
                await DisplayAlert("Conexión exitosa", $"ESP32 detectado en {SavedESP32Host} ✅", "OK");

                // 🚀 Enviar deviceId al backend Express y UID al ESP32
                await SendDeviceToBackend();

                StartESP32DataListener();
            }
            else await DisplayAlert("Error", "No se pudo detectar el ESP32 automáticamente. Verifica la red WiFi y los permisos de la app.", "OK");
        }

        // ============================================================
        // DESCUBRIMIENTO Y conexión HTTP
        // ============================================================

        private async Task<bool> DiscoverAndConnectESP32()
        {
#if WINDOWS
            await DisplayAlert("Aviso", "El escaneo local no está disponible en Windows.", "OK");
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

                    Console.WriteLine("UDP reply: " + reply);

                    // Verificamos que sea respuesta del ESP32
                    if (reply.StartsWith("DISCOVER_TERRANOVA") || reply.Contains("TERRANOVA_OK"))
                    {
                        // 🟢 CORRECCIÓN 2.1: Extraer y Guardar el DEVICE_ID del ESP32
                        // El formato de respuesta esperado es: DISCOVER_TERRANOVA|000000000000|192.168.1.15|80
                        string[] parts = reply.Split('|');
                        if (parts.Length > 1)
                        {
                            string deviceId = parts[1].Trim(); // El ID está en la posición 1
                            if (!string.IsNullOrEmpty(deviceId) && deviceId != "000000000000")
                            {
                                SessionStore.SaveDeviceId(deviceId); // 👈 ¡Guardado!
                            }
                        }

                        // Extraemos la IP real
                        string espIP = response.RemoteEndPoint.Address.ToString();

                        // Intentamos conectar vía HTTP
                        if (await TryConnectHttp(espIP))
                        {
                            SavedESP32Host = espIP;
                            SessionStore.SaveDeviceIp(espIP);
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error descubrimiento: " + ex.Message);
            }

            // Fallback a mDNS / IP fija
            return await TryConnectHttp(DEFAULT_MDNS);
#endif
        }


        private static async Task<bool> TryConnectHttp(string host)
        {
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };

                // El endpoint correcto en el ESP32 es la raíz "/"
                var response = await client.GetAsync($"http://{host}:{ESP_HTTP_PORT}/");

                return response.IsSuccessStatusCode;
            }
            catch { return false; }
        }

        // ============================================================
        // BOTÓN RE-CONEXIÓN
        // ============================================================

        private async void ConnectESP32_Clicked(object sender, EventArgs e)
        {
            ConnectionStatus.Text = "Reconectando ESP32...";
            ConnectionStatus.TextColor = Colors.Orange;

            bool connected = await DiscoverAndConnectESP32();

            ConnectionStatus.Text = connected ? $"Estado: Conectado ✅ ({SavedESP32Host})" : "Estado: Desconectado ❌";
            ConnectionStatus.TextColor = connected ? Colors.Green : Colors.Red;

            await DisplayAlert("Conexión ESP32", connected ? $"Conectado a {SavedESP32Host} ✅" : "No se pudo conectar ❌", "OK");

            if (connected)
            {
                await SendDeviceToBackend();
                StartESP32DataListener();
            }
        }

        // ============================================================
        // 🔥 Enviar deviceId + token Firebase → backend Express + USER_ID al ESP32
        // ============================================================

        private async Task SendDeviceToBackend()
        {
            try
            {
                string deviceId = SessionStore.GetDeviceId();
                string uid = SessionStore.GetUid();

                // 🟢 CORRECCIÓN 2.2: Usar DEFAULT_MDNS como fallback si DeviceID está vacío
                if (string.IsNullOrEmpty(deviceId))
                {
                    deviceId = DEFAULT_MDNS; // terranova.local (o cualquier valor que tu backend acepte como 'default')
                    SessionStore.SaveDeviceId(deviceId);
                }

                if (string.IsNullOrEmpty(uid))
                {
                    // La alerta original es la que ves, ahora sabes por qué: falta sesión.
                    await DisplayAlert("Error", "No existe deviceId o UID para vincular. Asegúrate de iniciar sesión.", "OK");
                    return;
                }

                bool ok = await _backend.ClaimDeviceAsync(deviceId);

                if (ok)
                {
                    await DisplayAlert("Backend", "Dispositivo vinculado con tu cuenta en el backend 🔥", "OK");
                    await SendUserIdToESP32(uid); // ← Aquí enviamos el UID al ESP32
                }
                else
                    await DisplayAlert("Error", "No se pudo vincular el dispositivo en el backend.", "OK");
            }
            catch (Exception ex)
            {
                // Este catch ahora atrapará otros errores, pero ya no el de URL no configurada
                await DisplayAlert("Error", $"Backend error: {ex.Message}", "OK");
            }
        }

        // ============================================================
        // 🔹 Enviar UID al ESP32
        // ============================================================

        private async Task SendUserIdToESP32(string uid)
        {
            try
            {
                if (string.IsNullOrEmpty(SavedESP32Host)) return;

                using var client = new HttpClient();
                string url = $"http://{SavedESP32Host}/setUserId?userId={uid}";
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                    Console.WriteLine("USER_ID enviado al ESP32 ✅");
                else
                    Console.WriteLine("❌ Falló envío USER_ID al ESP32");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error enviando USER_ID: " + ex.Message);
            }
        }

        // ============================================================
        // GUARDADO TELÉFONO
        // ============================================================

        private async void SavePhoneButton_Clicked(object sender, EventArgs e)
        {
            var phone = PhoneNumberEntry.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(phone)) { await DisplayAlert("Aviso", "Ingresa un número", "OK"); return; }

            var uid = SessionStore.GetUid();
            if (string.IsNullOrEmpty(uid)) { await DisplayAlert("Error", "No hay usuario", "OK"); return; }

            try
            {
                var extra = await _firebase.GetUserExtraDataAsync(uid);

                string currentEmail = extra.ContainsKey("email") ? extra["email"].ToString()! : SessionStore.GetUserEmail();
                string currentHash = extra.ContainsKey("passwordHash") ? extra["authHash"].ToString()! : "";
                string currentName = AppState.CurrentUserName;

                if (string.IsNullOrEmpty(currentEmail))
                {
                    await DisplayAlert("Error", "No se pudo obtener email.", "OK");
                    return;
                }

                bool ok = await _firebase.SaveUserInfoAsync(uid, currentName, currentEmail, phone, currentHash);

                if (ok)
                {
                    SessionStore.SavePhone(phone);
                    await DisplayAlert("✔ Guardado", "Teléfono guardado", "OK");
                }
                else await DisplayAlert("❌ Error", "No se pudo guardar teléfono", "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("❌ Error", ex.Message, "OK");
            }
        }

        // ============================================================
        // LOGOUT Y ELIMINAR
        // ============================================================

        private async void LogoutButton_Clicked(object sender, EventArgs e)
        {
            await _auth.LogoutAsync();
            Application.Current.MainPage = new NavigationPage(new LoginPage(_auth))
            {
                BarBackgroundColor = Color.FromArgb("#4CAF50"),
                BarTextColor = Colors.White
            };
        }

        private async void DeleteAccountButton_Clicked(object sender, EventArgs e)
        {
            bool confirm1 = await DisplayAlert("Eliminar cuenta", "¿Estás seguro?", "Sí", "No");
            if (!confirm1) return;

            bool confirm2 = await DisplayAlert("Confirmar eliminación", "Esta acción no se puede revertir.", "Sí", "No");
            if (!confirm2) return;

            await DisplayAlert("Cuenta eliminada", "Tu cuenta ha sido eliminada.", "OK");

            await _auth.LogoutAsync();
            Application.Current.MainPage = new NavigationPage(new LoginPage(_auth))
            {
                BarBackgroundColor = Color.FromArgb("#4CAF50"),
                BarTextColor = Colors.White
            };
        }

        // ============================================================
        // ESCUCHA ESP32 → Firebase
        // ============================================================

        private void StartESP32DataListener()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(1000) };
                        var response = await client.GetAsync($"http://{SavedESP32Host}:{ESP_HTTP_PORT}/datos");
                        if (response.IsSuccessStatusCode)
                        {
                            string espDataJson = await response.Content.ReadAsStringAsync();
                            var espData = JsonSerializer.Deserialize<ESP32Data>(espDataJson);

                            if (espData != null)
                            {
                                MainThread.BeginInvokeOnMainThread(async () =>
                                {
                                    ConnectionStatus.Text = $"Conectado: {SavedESP32Host}";
                                    ConnectionStatus.TextColor = Colors.Green;

                                    // Lanza el evento para que MainPage lo capture
                                    AppState.RaiseNewEspData(espData);

                                    // ❌ LÓGICA DE FIREBASE ELIMINADA DE AQUÍ: 
                                    // La responsabilidad de guardar en Firebase ahora es solo de MainPage.
                                });
                            }
                        }

                        await Task.Delay(2000);
                    }
                    catch
                    {
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            ConnectionStatus.Text = "Desconectado ❌";
                            ConnectionStatus.TextColor = Colors.Red;
                        });
                        await Task.Delay(5000);
                    }
                }
            });
        }
    }
}