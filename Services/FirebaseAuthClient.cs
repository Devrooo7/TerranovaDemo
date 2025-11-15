using System.Net.Http;
using System.Text;
using System.Text.Json;
using Firebase.Database;
using Firebase.Database.Query;
using Google.Cloud.Firestore;

namespace TerranovaDemo.Services
{
    public class FirebaseAuthClient
    {
        private readonly HttpClient _http;
        private readonly string _apiKey;
        private readonly FirebaseClient _realtimeDb;

#if WINDOWS || MACCATALYST
        private readonly FirestoreDb _firestore;
#endif

        public FirebaseAuthClient(string firebaseApiKey, string realtimeUrl)
        {
            _apiKey = firebaseApiKey;
            _http = new HttpClient();
            _realtimeDb = new FirebaseClient(realtimeUrl);

#if WINDOWS || MACCATALYST
            _firestore = FirestoreDb.Create("tu-proyecto");
#endif
        }

        // -------------------------------------------------------------
        // LOGIN USUARIO
        // -------------------------------------------------------------
        public async Task<(bool success, string localId, string idToken, string refreshToken, string? displayName)>
            LoginUserAsync(string email, string password)
        {
            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={_apiKey}",
                content);

            if (!response.IsSuccessStatusCode)
                return (false, "", "", "", null);

            var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

            return (
                true,
                json.GetProperty("localId").GetString()!,
                json.GetProperty("idToken").GetString()!,
                json.GetProperty("refreshToken").GetString()!,
                json.TryGetProperty("displayName", out var dn) ? dn.GetString() : null
            );
        }

        // -------------------------------------------------------------
        // REGISTRO USUARIO
        // -------------------------------------------------------------
        public async Task<(bool success, string uid)> RegisterAsync(string email, string password)
        {
            var payload = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(
                $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={_apiKey}",
                content);

            if (!response.IsSuccessStatusCode)
                return (false, "");

            var json = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync());

            return (
                true,
                json.GetProperty("localId").GetString()!
            );
        }

        // -------------------------------------------------------------
        // OBTENER NOMBRE Y EMAIL (REALTIME PRIORIDAD)
        // -------------------------------------------------------------
        public async Task<Dictionary<string, object>> GetUserExtraDataAsync(string uid)
        {
            try
            {
                var data = await _realtimeDb
                    .Child("users")
                    .Child(uid)
                    .OnceSingleAsync<dynamic>();

                if (data != null)
                {
                    var dict = new Dictionary<string, object>();

                    if (data.name != null)
                        dict["name"] = data.name.ToString();

                    if (data.email != null)
                        dict["email"] = data.email.ToString();

                    return dict;
                }
            }
            catch { }

#if WINDOWS || MACCATALYST
            try
            {
                var doc = await _firestore.Collection("users").Document(uid).GetSnapshotAsync();
                if (doc.Exists)
                    return doc.ToDictionary();
            }
            catch { }
#endif

            return new Dictionary<string, object>();
        }

        // -------------------------------------------------------------
        // GUARDAR NOMBRE
        // -------------------------------------------------------------
        public async Task SaveUserNameAsync(string uid, string name, string email)
        {
            await _realtimeDb
                .Child("users")
                .Child(uid)
                .PutAsync(new
                {
                    name,
                    email
                });
        }

        // -------------------------------------------------------------
        // GUARDAR PREFERENCIAS USUARIO (plant, region)
        // -------------------------------------------------------------
        public async Task SaveUserPreferencesAsync(string uid, string plant, string region)
        {
            try
            {
                await _realtimeDb
                    .Child("users")
                    .Child(uid)
                    .Child("preferences")
                    .PutAsync(new
                    {
                        plant,
                        region,
                        updatedAt = DateTime.UtcNow.ToString("s")
                    });
            }
            catch { }

#if WINDOWS || MACCATALYST
            try
            {
                var doc = _firestore
                    .Collection("users")
                    .Document(uid)
                    .Collection("preferences")
                    .Document("settings");

                var data = new Dictionary<string, object>
                {
                    { "plant", plant },
                    { "region", region },
                    { "updatedAt", Timestamp.GetCurrentTimestamp() }
                };

                await doc.SetAsync(data);
            }
            catch { }
#endif
        }
    }
}
