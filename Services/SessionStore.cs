using Microsoft.Maui.Storage;

namespace TerranovaDemo.Services
{
    public static class SessionStore
    {
        public static void SaveToken(string token) =>
            Preferences.Set("token", token);

        public static string GetToken() =>
            Preferences.Get("token", "");

        public static void SaveRefresh(string token) =>
            Preferences.Set("refresh", token);

        public static string GetRefresh() =>
            Preferences.Get("refresh", "");

        public static void SaveUid(string uid) =>
            Preferences.Set("uid", uid);

        public static string GetUid() =>
            Preferences.Get("uid", "");

        public static void SaveUserName(string name) =>
            Preferences.Set("user_name", name);

        public static string GetUserName() =>
            Preferences.Get("user_name", "Usuario");

        public static void ClearSession()
        {
            Preferences.Remove("token");
            Preferences.Remove("refresh");
            Preferences.Remove("uid");
            Preferences.Remove("user_name");
        }
    }
}
