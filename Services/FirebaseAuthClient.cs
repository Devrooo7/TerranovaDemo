using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Google.Cloud.Firestore;

#if ANDROID
using Android.Content.Res;
#endif

namespace TerranovaDemo.Services
{
    public class FirebaseAuthClient
    {
        private readonly string ApiKey = "AIzaSyAEQNaGohNG32f52DZPbgljT9Rz3w6O-bM"; // ⚠️ reemplaza con tu API Key real
        private readonly HttpClient _httpClient;
        private readonly FirebaseClient _db;

#if !ANDROID
        private readonly FirestoreDb _firestore;
#endif

        public FirebaseAuthClient()
        {
            _httpClient = new HttpClient();

            // Conexión a tu Realtime Database
            _db = new FirebaseClient("https://terranova-62f60-default-rtdb.firebaseio.com");

#if !ANDROID
            // Para Windows/Mac sigue usando tu JSON local
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",
                @"C:\Users\roo07\source\repos\TerranovaDemo\terranova-62f60-firebase-adminsdk-fbsvc-8d8749a75c.json");

            _firestore = FirestoreDb.Create("terranova-62f60");
#endif
        }

        // ------------------- 🔐 REGISTRO REAL -------------------
        public async Task<AuthResponse?> SignUpAsync(string email, string password, string name)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}";

            // ✅ corrección: se serializa correctamente el JSON con comillas válidas
            var data = new
            {
                email = email,
                password = password,
                returnSecureToken = true
            };

            var json = JsonSerializer.Serialize(data);
            var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error al registrar: {result}");
                return null;
            }

            var auth = JsonSerializer.Deserialize<AuthResponse>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

#if ANDROID
            // Guardar en Firestore usando REST API en Android
            await SaveUserInFirestoreAsync(auth.LocalId!, email, name, auth.IdToken!);
#else
            await SaveUserInDatabaseAsync(auth.LocalId!, email, name);
#endif

            return auth;
        }

        // ------------------- 🔑 LOGIN REAL -------------------
        public async Task<AuthResponse?> SignInAsync(string email, string password)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={ApiKey}";

            var data = new
            {
                email,
                password,
                returnSecureToken = true
            };

            var json = JsonSerializer.Serialize(data);
            var response = await _httpClient.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Error al iniciar sesión: {result}");
                return null;
            }

            var auth = JsonSerializer.Deserialize<AuthResponse>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            // Obtener nombre de Realtime o Firestore
            var userName = await GetUserNameFromDatabaseAsync(auth.LocalId!);
            auth.DisplayName = userName ?? "Usuario";

            return auth;
        }

        // ------------------- 🔹 GUARDAR USUARIO -------------------
        public async Task SaveUserInDatabaseAsync(string uid, string email, string? displayName)
        {
            await _db.Child("users").Child(uid).PutAsync(new
            {
                email,
                name = displayName ?? "",
                createdAt = DateTime.UtcNow.ToString("s")
            });

#if !ANDROID
            var docRef = _firestore.Collection("users").Document(uid);
            await docRef.SetAsync(new Dictionary<string, object>
            {
                { "email", email },
                { "name", displayName ?? "" },
                { "createdAt", Timestamp.GetCurrentTimestamp() }
            });
#endif
        }

        // ------------------- 🔹 OBTENER NOMBRE -------------------
        public async Task<string?> GetUserNameFromDatabaseAsync(string uid)
        {
            try
            {
                var userData = await _db.Child("users").Child(uid).OnceSingleAsync<dynamic>();
                if (userData != null && userData.name != null)
                    return userData.name.ToString();

#if !ANDROID
                var doc = await _firestore.Collection("users").Document(uid).GetSnapshotAsync();
                if (doc.Exists && doc.ContainsField("name"))
                    return doc.GetValue<string>("name");
#endif

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ------------------- 🔹 GUARDAR PREFERENCIAS -------------------
        public async Task SaveUserPreferencesAsync(string uid, string plant, string region, string? idToken = null)
        {
            await _db.Child("users").Child(uid).Child("preferences").PutAsync(new { plant, region });

#if ANDROID
            if (!string.IsNullOrEmpty(idToken))
            {
                await SaveUserPreferencesInFirestoreAsync(uid, plant, region, idToken);
            }
#else
            var docRef = _firestore.Collection("users").Document(uid)
                                   .Collection("preferences").Document("settings");

            await docRef.SetAsync(new Dictionary<string, object>
            {
                { "plant", plant },
                { "region", region },
                { "updatedAt", Timestamp.GetCurrentTimestamp() }
            });
#endif
        }

        // ------------------- 🔹 DATOS EXTRA -------------------
        public async Task<Dictionary<string, object>> GetUserExtraDataAsync(string uid)
        {
#if ANDROID
            return new Dictionary<string, object>(); // Opcional: REST API para Android
#else
            var docRef = _firestore.Collection("users").Document(uid);
            var snapshot = await docRef.GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ToDictionary() : new Dictionary<string, object>();
#endif
        }

#if ANDROID
        // ------------------- 🔹 FIRESTORE REST API ANDROID -------------------
        private async Task SaveUserInFirestoreAsync(string uid, string email, string? displayName, string idToken)
        {
            var firestoreUrl = $"https://firestore.googleapis.com/v1/projects/terranova-62f60/databases/(default)/documents/users/{uid}";
            var payload = new
            {
                fields = new Dictionary<string, object>
                {
                    { "email", new { stringValue = email } },
                    { "name", new { stringValue = displayName ?? "" } },
                    { "createdAt", new { stringValue = DateTime.UtcNow.ToString("s") } }
                }
            };
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Patch, firestoreUrl + "?currentDocument.exists=false")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);
            await _httpClient.SendAsync(request);
        }

        private async Task SaveUserPreferencesInFirestoreAsync(string uid, string plant, string region, string idToken)
        {
            var firestoreUrl = $"https://firestore.googleapis.com/v1/projects/terranova-62f60/databases/(default)/documents/users/{uid}/preferences/settings";
            var payload = new
            {
                fields = new Dictionary<string, object>
                {
                    { "plant", new { stringValue = plant } },
                    { "region", new { stringValue = region } },
                    { "updatedAt", new { stringValue = DateTime.UtcNow.ToString("s") } }
                }
            };
            var json = JsonSerializer.Serialize(payload);
            var request = new HttpRequestMessage(HttpMethod.Patch, firestoreUrl + "?currentDocument.exists=false")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", idToken);
            await _httpClient.SendAsync(request);
        }
#endif

        // ------------------- 🔹 MODELO -------------------
        public class AuthResponse
        {
            public string? IdToken { get; set; }
            public string? Email { get; set; }
            public string? RefreshToken { get; set; }
            public string? ExpiresIn { get; set; }
            public string? LocalId { get; set; }
            public string? DisplayName { get; set; }
        }
    }
}
