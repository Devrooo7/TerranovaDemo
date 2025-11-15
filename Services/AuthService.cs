using Microsoft.Maui.Storage;

namespace TerranovaDemo.Services
{
    public class AuthService
    {
        private readonly FirebaseAuthClient _firebase;

        public AuthService(FirebaseAuthClient firebase)
        {
            _firebase = firebase;
        }

        // -------------------------------------------------------------
        // LOGIN
        // -------------------------------------------------------------
        public async Task<bool> LoginAsync(string email, string password)
        {
            var res = await _firebase.LoginUserAsync(email, password);

            if (!res.success)
                return false;

            // Guardar tokens
            SessionStore.SaveToken(res.idToken);
            SessionStore.SaveRefresh(res.refreshToken);
            SessionStore.SaveUid(res.localId);

            // Nombre
            var extra = await _firebase.GetUserExtraDataAsync(res.localId);
            string finalName =
                extra.ContainsKey("name") ? extra["name"].ToString()! :
                res.displayName ?? "Usuario";

            SessionStore.SaveUserName(finalName);

            AppState.CurrentUserUid = res.localId;
            AppState.CurrentUserName = finalName;
            AppState.IsLogged = true;

            return true;
        }

        // -------------------------------------------------------------
        // REGISTRO
        // -------------------------------------------------------------
        public async Task<bool> RegisterUser(string email, string password, string displayName)
        {
            var result = await _firebase.RegisterAsync(email, password);
            if (!result.success)
                return false;

            await _firebase.SaveUserNameAsync(result.uid, displayName, email);

            SessionStore.SaveUid(result.uid);
            SessionStore.SaveUserName(displayName);

            AppState.CurrentUserUid = result.uid;
            AppState.CurrentUserName = displayName;
            AppState.IsLogged = true;

            return true;
        }

        // -------------------------------------------------------------
        // LOGOUT
        // -------------------------------------------------------------
        public async Task LogoutAsync()
        {
            SessionStore.ClearSession();

            AppState.IsLogged = false;
            AppState.CurrentUserUid = string.Empty;
            AppState.CurrentUserName = string.Empty;

            await Task.CompletedTask;
        }

        // -------------------------------------------------------------
        // OBTENER NOMBRE
        // -------------------------------------------------------------
        public async Task<string> GetUserNameAsync()
        {
            var uid = SessionStore.GetUid();
            if (string.IsNullOrEmpty(uid))
                return "Usuario";

            var extra = await _firebase.GetUserExtraDataAsync(uid);

            if (extra.ContainsKey("name"))
                return extra["name"].ToString()!;

            return SessionStore.GetUserName();
        }
    }
}
