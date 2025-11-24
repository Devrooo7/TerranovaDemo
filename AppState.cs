using TerranovaDemo.Services;

namespace TerranovaDemo
{
    public static class AppState
    {
        // Configuración del ESP32
        public static string SavedESP32Ip { get; set; } = string.Empty;
        public static string SavedPhoneNumber { get; set; } = string.Empty;

        // Firebase
        public static string CurrentUserUid { get; set; } = string.Empty;
        public static string CurrentUserName { get; set; } = string.Empty;
        public static string CurrentUserEmail { get; set; } = string.Empty;
        public static string SavedDeviceId { get; set; } = string.Empty;

        // Datos de agricultura
        public static string PlantType { get; set; } = string.Empty;
        public static string Region { get; set; } = string.Empty;

        public static bool IsLogged { get; set; } = false;

        // Helper rápido
        public static bool HasUserSession =>
            !string.IsNullOrEmpty(CurrentUserUid) &&
            IsLogged;

        // Evento para notificar nuevas lecturas ESP32
        public static event System.Action<ESP32Data>? OnNewEspData;

        public static void RaiseNewEspData(ESP32Data data) => OnNewEspData?.Invoke(data);
    }
}
