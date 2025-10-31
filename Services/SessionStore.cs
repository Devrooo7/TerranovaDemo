using Microsoft.Maui.Storage;
using System.Threading.Tasks;

namespace TerranovaDemo.Services
{
    public static class SessionStore
    {
        private const string KEY_IDTOKEN = "fb_idToken";
        private const string KEY_REFRESH = "fb_refreshToken";
        private const string KEY_UID = "fb_uid";
        private const string KEY_EMAIL = "fb_email";
        private const string KEY_NAME = "fb_displayName";

        public static async Task SaveAsync(string? idToken, string? refresh, string? uid, string? email, string? name)
        {
            await SecureStorage.SetAsync(KEY_IDTOKEN, idToken ?? string.Empty);
            await SecureStorage.SetAsync(KEY_REFRESH, refresh ?? string.Empty);
            await SecureStorage.SetAsync(KEY_UID, uid ?? string.Empty);
            await SecureStorage.SetAsync(KEY_EMAIL, email ?? string.Empty);
            await SecureStorage.SetAsync(KEY_NAME, name ?? string.Empty);
        }

        public static Task<string?> GetIdTokenAsync() => SecureStorage.GetAsync(KEY_IDTOKEN);

        public static async Task ClearAsync()
        {
            SecureStorage.Remove(KEY_IDTOKEN);
            SecureStorage.Remove(KEY_REFRESH);
            SecureStorage.Remove(KEY_UID);
            SecureStorage.Remove(KEY_EMAIL);
            SecureStorage.Remove(KEY_NAME);
            await Task.CompletedTask;
        }
    }
}
