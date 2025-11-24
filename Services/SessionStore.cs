using Microsoft.Maui.Storage;

namespace TerranovaDemo.Services
{
    public static class SessionStore
    {
        private const string TOKEN = "token";
        private const string REFRESH = "refresh";
        private const string UID = "uid";
        private const string USERNAME = "username";
        private const string USEREMAIL = "user_email";
        private const string DEVICE_ID = "device_id";
        private const string DEVICE_IP = "device_ip";
        private const string USER_PHONE = "user_phone";

        // ================= SAVE/GET INDIVIDUAL =================

        public static void SaveToken(string token) => Preferences.Set(TOKEN, token);
        public static string GetToken() => Preferences.Get(TOKEN, "");

        public static void SaveRefresh(string refresh) => Preferences.Set(REFRESH, refresh);
        public static string GetRefresh() => Preferences.Get(REFRESH, "");

        public static void SaveUid(string uid) => Preferences.Set(UID, uid);
        public static string GetUid() => Preferences.Get(UID, "");

        public static void SaveUserName(string name) => Preferences.Set(USERNAME, name);
        public static string GetUserName() => Preferences.Get(USERNAME, "");

        public static void SaveUserEmail(string email) => Preferences.Set(USEREMAIL, email);
        public static string GetUserEmail() => Preferences.Get(USEREMAIL, "");

        public static void SaveDeviceId(string deviceId) => Preferences.Set(DEVICE_ID, deviceId);
        public static string GetDeviceId() => Preferences.Get(DEVICE_ID, "");

        public static void SaveDeviceIp(string ip) => Preferences.Set(DEVICE_IP, ip);
        public static string GetDeviceIp() => Preferences.Get(DEVICE_IP, "");

        public static void SavePhone(string phone) => Preferences.Set(USER_PHONE, phone);
        public static string GetPhone() => Preferences.Get(USER_PHONE, "");

        // ================= NUEVOS MÉTODOS DE SESIÓN =================

        /// <summary>
        /// Guarda el UID, Token, Refresh Token y Email después de Login/Register.
        /// </summary>
        public static void SaveCredentials(string uid, string token, string refresh, string email)
        {
            SaveUid(uid);
            SaveToken(token);
            SaveRefresh(refresh);
            SaveUserEmail(email);
        }

        public static void ClearSession()
        {
            Preferences.Remove(TOKEN);
            Preferences.Remove(REFRESH);
            Preferences.Remove(UID);
            Preferences.Remove(USERNAME);
            Preferences.Remove(USEREMAIL);
            Preferences.Remove(DEVICE_ID);
            Preferences.Remove(DEVICE_IP);
            Preferences.Remove(USER_PHONE);
        }
    }
}