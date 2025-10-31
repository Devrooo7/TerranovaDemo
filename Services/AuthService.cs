using System.Threading.Tasks;

namespace TerranovaDemo.Services
{
    public static class AuthService
    {
        private static readonly FirebaseAuthClient _firebaseClient = new FirebaseAuthClient();

        public static async Task<bool> LoginUser(string email, string password)
        {
            var res = await _firebaseClient.SignInAsync(email, password);
            if (res != null)
            {
                AppState.CurrentUserUid = res.LocalId ?? "";
                AppState.CurrentUserName = res.DisplayName ?? "Usuario";

                // Recuperar nombre real desde Firestore o Realtime Database
                try
                {
                    var userData = await _firebaseClient.GetUserExtraDataAsync(res.LocalId!);
                    if (userData != null && userData.ContainsKey("name"))
                        AppState.CurrentUserName = userData["name"].ToString();
                }
                catch { }

                return true;
            }
            return false;
        }

        public static async Task<bool> RegisterUser(string email, string password, string displayName)
        {
            var res = await _firebaseClient.SignUpAsync(email, password, displayName);
            if (res != null)
            {
                AppState.CurrentUserUid = res.LocalId ?? "";
                AppState.CurrentUserName = displayName;

                await _firebaseClient.SaveUserInDatabaseAsync(res.LocalId!, email, displayName);
                return true;
            }
            return false;
        }

        public static async Task LogoutAsync()
        {
            AppState.CurrentUserUid = "";
            AppState.CurrentUserName = "";
            await SessionStore.ClearAsync();
        }
    }
}
