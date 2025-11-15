namespace TerranovaDemo
{
    public static class AppState
    {
        public static string SavedESP32Ip { get; set; } = string.Empty;
        public static string SavedPhoneNumber { get; set; } = string.Empty;

        // Sesión Firebase
        public static string CurrentUserUid { get; set; } = string.Empty;
        public static string CurrentUserName { get; set; } = string.Empty;

        // Datos de agricultura
        public static string PlantType { get; set; } = string.Empty;
        public static string Region { get; set; } = string.Empty;

        // Bandera de login persistente
        public static bool IsLogged { get; set; } = false;

        // ---- Helper opcional ----
        public static bool HasUserSession =>
            !string.IsNullOrEmpty(CurrentUserUid) &&
            IsLogged;
    }
}
