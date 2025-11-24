using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

// (Asumo que CryptoHelper está en tu proyecto)
// using TerranovaDemo.Utils; 

namespace TerranovaDemo.Services
{
    public class FirebaseAuthClient
    {
        private readonly string _apiKey = "AIzaSyDz6ImYD2-iMa15c09AYarAD0iwnmAx3n8";
        private readonly string _baseUrl = "https://terranova-62f60-default-rtdb.firebaseio.com";
        private readonly HttpClient _http = new HttpClient();

        private readonly FirestoreDb? _firestore;
        private readonly string _projectId = "terranova-62f60";

        // ⚠ NOTA: Reemplaza esto con un Service Account Token real 
        // si el ESP32 no usa la autenticación de Firebase del usuario.
        private readonly string _serviceToken = "<TOKEN_DE_SERVICIO>";

        public FirebaseAuthClient()
        {
            try
            {
                // Asegúrate de que las credenciales de Firestore estén configuradas correctamente 
                // para que esta línea no lance una excepción en un entorno de desarrollo/producción real.
                _firestore = FirestoreDb.Create(_projectId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠ Firestore init failed: " + ex.Message);
                _firestore = null;
            }
        }

        // ================= LOGIN =================
        public async Task<(bool success, string localId, string idToken, string refreshToken)> LoginUserAsync(string email, string password)
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}";
            var payload = new { email, password, returnSecureToken = true };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content);

            if (!response.IsSuccessStatusCode) return (false, "", "", "");

            var json = JsonSerializer.Deserialize<Dictionary<string, object>>(await response.Content.ReadAsStringAsync())!;

            string localId = json["localId"].ToString()!;
            string idToken = json["idToken"].ToString()!;
            string refreshToken = json["refreshToken"].ToString()!;

            // Guardar credenciales al iniciar sesión.
            SessionStore.SaveCredentials(localId, idToken, refreshToken, email);

            return (true, localId, idToken, refreshToken);
        }

        // ================= REGISTRO (CORREGIDO) =================
        public async Task<(bool success, string uid)> RegisterAsync(string email, string password, string name, string phone = "")
        {
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}";
            var payload = new { email, password, returnSecureToken = true };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(url, content);

            if (!response.IsSuccessStatusCode) return (false, "");

            var json = JsonSerializer.Deserialize<Dictionary<string, object>>(await response.Content.ReadAsStringAsync())!;
            string uid = json["localId"].ToString()!;
            string idToken = json["idToken"].ToString()!; // 💡 Obtener el ID Token
            string refreshToken = json["refreshToken"].ToString()!;

            // 💥 CORRECCIÓN: Guardar el token ANTES de llamar a SaveUserInfoAsync
            SessionStore.SaveCredentials(uid, idToken, refreshToken, email);

            // Asumo que tienes una clase CryptoHelper en tu proyecto
            string passwordHash = CryptoHelper.HashPassword(password);

            // Ahora, SaveUserInfoAsync usará el token recién guardado
            await SaveUserInfoAsync(uid, name, email, phone, passwordHash);

            return (true, uid);
        }

        // ================= GUARDAR DATOS DE USUARIO =================
        public async Task<bool> SaveUserInfoAsync(string uid, string? name = null, string? email = null, string? phone = null, string? passwordHash = null)
        {
            try
            {
                var currentData = await GetUserExtraDataAsync(uid);

                string finalName = !string.IsNullOrEmpty(name) ? name : currentData.GetValueOrDefault("name")?.ToString() ?? "Usuario";
                string finalEmail = !string.IsNullOrEmpty(email) ? email : currentData.GetValueOrDefault("email")?.ToString() ?? "";
                string finalPhone = !string.IsNullOrEmpty(phone) ? phone : currentData.GetValueOrDefault("phone")?.ToString() ?? "";
                string finalPasswordHash = !string.IsNullOrEmpty(passwordHash) ? passwordHash : currentData.GetValueOrDefault("passwordHash")?.ToString() ?? "";

                // Realtime Database
                var payload = new
                {
                    name = finalName,
                    email = finalEmail,
                    telefono = finalPhone,
                    passwordHash = finalPasswordHash,
                    fechaRegistro = currentData.GetValueOrDefault("fechaRegistro")?.ToString() ?? DateTime.UtcNow.ToString("o"),
                    ultimaConexion = DateTime.UtcNow.ToString("o")
                };

                string token = SessionStore.GetToken();
                string url = $"{_baseUrl}/users/{uid}.json";
                if (!string.IsNullOrEmpty(token)) url += $"?auth={token}";

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await _http.PatchAsync(url, content);
                bool rtOk = res.IsSuccessStatusCode;

                // ... (Resto de la lógica de Firestore) ...
                bool fsOk = true;
                try
                {
                    if (_firestore != null)
                    {
                        var docRef = _firestore.Collection("users").Document(uid);

                        var fsPayload = new Dictionary<string, object>
                        {
                            { "name", finalName },
                            { "email", finalEmail },
                            { "phone", finalPhone },
                            { "passwordHash", finalPasswordHash },
                            { "createdAt", currentData.ContainsKey("createdAt") ? currentData["createdAt"] : Timestamp.GetCurrentTimestamp() }
                        };

                        await docRef.SetAsync(fsPayload, SetOptions.MergeAll);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("⚠ Firestore SaveUserInfoAsync error: " + ex.Message);
                    fsOk = false;
                }

                return rtOk && fsOk;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR SaveUserInfoAsync → " + ex.Message);
                return false;
            }
        }

        // ================= GUARDAR PREFERENCIAS =================
        public async Task<bool> SaveUserPreferencesAsync(string uid, string plant, string region)
        {
            // ... (Este método funciona correctamente usando SessionStore.GetToken()) ...
            try
            {
                string token = SessionStore.GetToken();
                string url = $"{_baseUrl}/users/{uid}/preferences.json";
                if (!string.IsNullOrEmpty(token)) url += $"?auth={token}";

                var payload = new
                {
                    plant,
                    region,
                    updatedAt = DateTime.UtcNow.ToString("o")
                };

                var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var res = await _http.PatchAsync(url, content);
                bool rtOk = res.IsSuccessStatusCode;

                bool fsOk = true;
                try
                {
                    if (_firestore != null)
                    {
                        var docRef = _firestore.Collection("users").Document(uid);

                        var dict = new Dictionary<string, object>
                        {
                            { "preferences", new Dictionary<string, object>
                                {
                                    { "plant", plant },
                                    { "region", region },
                                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                                }
                            }
                        };

                        await docRef.SetAsync(dict, SetOptions.MergeAll);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("⚠ Firestore SaveUserPreferencesAsync error: " + ex.Message);
                    fsOk = false;
                }

                return rtOk && fsOk;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR SaveUserPreferencesAsync → " + ex.Message);
                return false;
            }
        }

        // ================= VINCULAR DISPOSITIVO =================
        public async Task<bool> LinkDeviceToUserAsync(string uid, string deviceId, string deviceIp)
        {
            // ... (Este método funciona correctamente usando SessionStore.GetToken()) ...
            try
            {
                string token = SessionStore.GetToken();

                var devPayload = new
                {
                    deviceId,
                    ip = deviceIp,
                    usuarioAsignado = uid,
                    ultimaLectura = new { temperatura = 0, humedadSuelo = 0, bomba = false, timestamp = DateTime.UtcNow.ToString("o") }
                };
                string urlDev = $"{_baseUrl}/dispositivos/{deviceId}.json";
                if (!string.IsNullOrEmpty(token)) urlDev += $"?auth={token}";
                await _http.PatchAsync(urlDev, new StringContent(JsonSerializer.Serialize(devPayload), Encoding.UTF8, "application/json"));

                var userDevPayload = new
                {
                    linked = true,
                    deviceId,
                    ip = deviceIp,
                    linkedAt = DateTime.UtcNow.ToString("o"),
                    lecturas = new Dictionary<string, object>(),
                    lastPump = new { state = false, fecha = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") }
                };
                string urlUserDev = $"{_baseUrl}/users/{uid}/dispositivos/{deviceId}.json";
                if (!string.IsNullOrEmpty(token)) urlUserDev += $"?auth={token}";
                var res2 = await _http.PatchAsync(urlUserDev, new StringContent(JsonSerializer.Serialize(userDevPayload), Encoding.UTF8, "application/json"));
                bool rtOk = res2.IsSuccessStatusCode;

                bool fsOk = true;
                try
                {
                    if (_firestore != null)
                    {
                        var devDoc = _firestore.Collection("users").Document(uid)
                            .Collection("dispositivos").Document(deviceId);
                        var dict = new Dictionary<string, object>
                        {
                            { "linked", true },
                            { "deviceId", deviceId },
                            { "ip", deviceIp },
                            { "linkedAt", Timestamp.GetCurrentTimestamp() }
                        };
                        await devDoc.SetAsync(dict, SetOptions.MergeAll);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("⚠ Firestore LinkDeviceToUserAsync error: " + ex.Message);
                    fsOk = false;
                }

                return rtOk && fsOk;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR LinkDeviceToUserAsync → " + ex.Message);
                return false;
            }
        }

        // ================= GUARDAR DATOS DEL ESP32 (Token de servicio si no hay token de usuario) =================
        public async Task<bool> SaveSensorDataAsync(string uid, string deviceId, int humedadSuelo, float temperatura, bool bombaStatus, string deviceIp = "")
        {
            try
            {
                // 💡 Estrategia de Token: Usa el token de usuario si está disponible, sino el token de servicio.
                string token = SessionStore.GetToken();
                if (string.IsNullOrEmpty(token)) token = _serviceToken;
                if (string.IsNullOrEmpty(token))
                {
                    Console.WriteLine("ERROR SaveSensorDataAsync: No hay token de usuario ni de servicio configurado.");
                    return false;
                }

                string fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // 1. Guardar Lectura Específica (POST)
                var lectura = new Dictionary<string, object>
                {
                    { "humedad", humedadSuelo },
                    { "temperatura", temperatura },
                    { "bomba", bombaStatus },
                    { "fecha", fecha }
                };

                string urlLectura = $"{_baseUrl}/users/{uid}/dispositivos/{deviceId}/lecturas.json?auth={token}";
                var resPost = await _http.PostAsync(urlLectura, new StringContent(JsonSerializer.Serialize(lectura), Encoding.UTF8, "application/json"));
                bool rtOk = resPost.IsSuccessStatusCode;

                // 2. Actualizar Última Lectura (PUT)
                var ultima = new
                {
                    temperatura,
                    humedadSuelo,
                    bomba = bombaStatus,
                    timestamp = DateTime.UtcNow.ToString("o"),
                    ip = deviceIp,
                    usuarioAsignado = uid
                };
                string urlUlt = $"{_baseUrl}/dispositivos/{deviceId}/ultimaLectura.json?auth={token}";
                var resUlt = await _http.PutAsync(urlUlt, new StringContent(JsonSerializer.Serialize(ultima), Encoding.UTF8, "application/json"));
                rtOk &= resUlt.IsSuccessStatusCode;

                // 3. Guardar en Historial Diario (PUT)
                var now = DateTime.UtcNow;
                string fechaDay = now.ToString("yyyy-MM-dd");
                string hora = now.ToString("HH-mm-ss");
                string urlHist = $"{_baseUrl}/users/{uid}/historial/{fechaDay}/{hora}.json?auth={token}";
                var histPayload = new
                {
                    deviceId,
                    temperatura,
                    humedadSuelo,
                    bomba = bombaStatus,
                    timestamp = now.ToString("o"),
                    ip = deviceIp
                };
                var resHist = await _http.PutAsync(urlHist, new StringContent(JsonSerializer.Serialize(histPayload), Encoding.UTF8, "application/json"));
                rtOk &= resHist.IsSuccessStatusCode;

                // Lógica de Firestore (funciona igual)
                bool fsOk = true;
                try
                {
                    if (_firestore != null)
                    {
                        var lecturesCol = _firestore.Collection("users").Document(uid)
                            .Collection("dispositivos").Document(deviceId).Collection("lecturas");

                        var fsLect = new Dictionary<string, object>
                        {
                            { "humedad", humedadSuelo },
                            { "temperatura", temperatura },
                            { "bomba", bombaStatus },
                            { "fecha", fecha },
                            { "timestamp", Timestamp.GetCurrentTimestamp() },
                            { "ip", deviceIp }
                        };
                        await lecturesCol.AddAsync(fsLect);

                        var pumpDoc = _firestore.Collection("users").Document(uid)
                            .Collection("dispositivos").Document(deviceId)
                            .Collection("status").Document("lastPump");
                        var pumpObj = new Dictionary<string, object>
                        {
                            { "state", bombaStatus },
                            { "fecha", fecha },
                            { "updatedAt", Timestamp.GetCurrentTimestamp() }
                        };
                        await pumpDoc.SetAsync(pumpObj, SetOptions.MergeAll);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("⚠ Firestore SaveSensorDataAsync error: " + ex.Message);
                    fsOk = false;
                }

                return rtOk && fsOk;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR SaveSensorDataAsync → " + ex.Message);
                return false;
            }
        }

        // ================= ACTUALIZAR ESTADO DE BOMBA =================
        public async Task<bool> UpdatePumpStateAsync(string uid, string deviceId, bool bombaStatus)
        {
            // ... (Este método también usa la lógica de fallback de token) ...
            try
            {
                string token = SessionStore.GetToken();
                if (string.IsNullOrEmpty(token)) token = _serviceToken;
                if (string.IsNullOrEmpty(token)) return false;

                string fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                var payload = new
                {
                    state = bombaStatus,
                    fecha,
                    updatedAt = DateTime.UtcNow.ToString("o")
                };

                string url = $"{_baseUrl}/users/{uid}/dispositivos/{deviceId}/lastPump.json?auth={token}";
                var res = await _http.PutAsync(url, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
                return res.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR UpdatePumpStateAsync → " + ex.Message);
                return false;
            }
        }

        // ================= OBTENER DATOS EXTRA DEL USUARIO =================
        public async Task<Dictionary<string, object>> GetUserExtraDataAsync(string uid)
        {
            // ... (Este método funciona correctamente usando SessionStore.GetToken()) ...
            try
            {
                string token = SessionStore.GetToken();
                string url = $"{_baseUrl}/users/{uid}.json";
                if (!string.IsNullOrEmpty(token)) url += $"?auth={token}";

                var res = await _http.GetAsync(url);
                if (!res.IsSuccessStatusCode) return new Dictionary<string, object>();

                var json = await res.Content.ReadAsStringAsync();
                var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                return dict ?? new Dictionary<string, object>();
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠ GetUserExtraDataAsync error: " + ex.Message);
                return new Dictionary<string, object>();
            }
        }
    }
}