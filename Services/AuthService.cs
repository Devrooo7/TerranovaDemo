using System.Threading.Tasks;

namespace TerranovaDemo.Services
{
    public class AuthService
    {
        private readonly FirebaseAuthClient _firebase;

        public AuthService(FirebaseAuthClient firebase)
        {
            _firebase = firebase;
        }

        // LOGIN: devuelve true si ok y guarda sesión
        public async Task<bool> LoginAsync(string email, string password)
        {
            var res = await _firebase.LoginUserAsync(email, password);
            if (!res.success) return false;

            // 🟢 CORRECCIÓN 1.1: Guardar el email en SessionStore
            SessionStore.SaveToken(res.idToken);
            SessionStore.SaveRefresh(res.refreshToken);
            SessionStore.SaveUid(res.localId);
            SessionStore.SaveUserEmail(email); // 👈 ¡Añadida!

            var extra = await _firebase.GetUserExtraDataAsync(res.localId);
            string finalName = extra.ContainsKey("name") ? extra["name"].ToString()! : "Usuario";

            SessionStore.SaveUserName(finalName);

            AppState.CurrentUserUid = res.localId;
            AppState.CurrentUserName = finalName;
            AppState.IsLogged = true;

            return true;
        }

        // REGISTRO: delega en FirebaseAuthClient.RegisterAsync (que guarda RT + FS)
        public async Task<bool> RegisterUserAsync(string email, string password, string name, string phone = "")
        {
            var result = await _firebase.RegisterAsync(email, password, name, phone);
            if (!result.success) return false;

            // 🟢 CORRECCIÓN 1.2: Guardar el email en SessionStore
            SessionStore.SaveUid(result.uid);
            SessionStore.SaveUserName(name);
            SessionStore.SaveUserEmail(email); // 👈 ¡Añadida!

            AppState.CurrentUserUid = result.uid;
            AppState.CurrentUserName = name;
            AppState.IsLogged = true;

            return true;
        }

        public async Task LogoutAsync()
        {
            SessionStore.ClearSession();
            AppState.IsLogged = false;
            AppState.CurrentUserUid = "";
            AppState.CurrentUserName = "";
            await Task.CompletedTask;
        }

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

        public FirebaseAuthClient GetFirebaseClient() => _firebase;
    }
}