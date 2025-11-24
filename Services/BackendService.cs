using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TerranovaDemo.Services;
using Microsoft.Maui.Storage; // ?? Asegúrate de tener este using

namespace TerranovaDemo.Backend
{
    public class BackendService
    {
        private readonly HttpClient _http;

        public BackendService()
        {
            _http = new HttpClient();
        }

        // ===============================
        // 1) Guardar y obtener URL backend
        // ===============================
        private const string BACKEND_KEY = "backend_url";

        public static void SaveBackendUrl(string url)
        {
            Preferences.Set(BACKEND_KEY, url);
        }

        public static string GetBackendUrl()
        {
            // Usará la URL guardada previamente.
            return Preferences.Get(BACKEND_KEY, "");
        }

        // =====================================================
        // 2) Reclamar dispositivo (deviceId + idToken de Firebase)
        // =====================================================
        public async Task<bool> ClaimDeviceAsync(string deviceId)
        {
            string backend = GetBackendUrl();
            if (string.IsNullOrEmpty(backend))
                throw new Exception("No se ha configurado la URL del backend."); // ?? ESTE ES EL ERROR QUE ESTÁS VIENDO ??

            string idToken = SessionStore.GetToken();
            if (string.IsNullOrEmpty(idToken))
                throw new Exception("No hay token de usuario.");

            // payload que tu Express espera
            var payload = new
            {
                deviceId = deviceId,
                idToken = idToken
            };

            string json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{backend}/devices/claim", content);

            return response.IsSuccessStatusCode;
        }

        // ... [Resto de la clase sin cambios]

        // =====================================================
        // 3) Enviar datos manualmente al backend (opcional)
        // =====================================================
        public async Task<bool> SendSensorDataAsync(
            float temperature,
            int soilPct,
            bool bomba,
            string deviceId,
            string userId)
        {
            string backend = GetBackendUrl();
            if (string.IsNullOrEmpty(backend))
                throw new Exception("No se ha configurado la URL del backend.");

            var payload = new
            {
                temperature = temperature,
                soilPct = soilPct,
                bomba = bomba,
                deviceId = deviceId,
                userId = userId
            };

            string json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _http.PostAsync($"{backend}/sensor", content);

            return response.IsSuccessStatusCode;
        }

        // =====================================================
        // 4) Verificar si el backend está vivo
        // =====================================================
        public async Task<bool> CheckHealthAsync()
        {
            string backend = GetBackendUrl();
            if (string.IsNullOrEmpty(backend))
                return false;

            try
            {
                var response = await _http.GetAsync($"{backend}/health");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}