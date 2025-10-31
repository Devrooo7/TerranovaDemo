using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Google.Cloud.Firestore;

namespace TerranovaDemo.Services
{
    public class FirebaseAuthClient
    {
        private readonly string ApiKey = "AIzaSyAEQNaGohNG32f52DZPbgljT9Rz3w6O-bM"; // ⚠️ reemplaza con tu API Key real
        private readonly HttpClient _httpClient;
        private readonly FirebaseClient _db;
        private readonly FirestoreDb _firestore;

        public FirebaseAuthClient()
        {
            _httpClient = new HttpClient();

            // Conexión a tu Realtime Database
            _db = new FirebaseClient("https://terranova-62f60-default-rtdb.firebaseio.com");

            // Conexión a Firestore con tus credenciales JSON
            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS",
                @"C:\Users\roo07\source\repos\TerranovaDemo\terranova-62f60-firebase-adminsdk-fbsvc-8d8749a75c.json");

            _firestore = FirestoreDb.Create("terranova-62f60");
        }

        // ------------------- 🔐 REGISTRO REAL -------------------
        public async Task<AuthResponse?> SignUpAsync(string email, string password, string name)
        {
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={ApiKey}";

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
                Console.WriteLine($"❌ Error al registrar: {result}");
                return null;
            }

            var auth = JsonSerializer.Deserialize<AuthResponse>(result, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            await SaveUserInDatabaseAsync(auth.LocalId!, email, name);
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

            var docRef = _firestore.Collection("users").Document(uid);
            await docRef.SetAsync(new Dictionary<string, object>
            {
                { "email", email },
                { "name", displayName ?? "" },
                { "createdAt", Timestamp.GetCurrentTimestamp() }
            });
        }

        // ------------------- 🔹 OBTENER NOMBRE -------------------
        public async Task<string?> GetUserNameFromDatabaseAsync(string uid)
        {
            try
            {
                var userData = await _db.Child("users").Child(uid).OnceSingleAsync<dynamic>();
                if (userData != null && userData.name != null)
                    return userData.name.ToString();

                var doc = await _firestore.Collection("users").Document(uid).GetSnapshotAsync();
                if (doc.Exists && doc.ContainsField("name"))
                    return doc.GetValue<string>("name");

                return null;
            }
            catch
            {
                return null;
            }
        }

        // ------------------- 🔹 GUARDAR PREFERENCIAS -------------------
        public async Task SaveUserPreferencesAsync(string uid, string plant, string region)
        {
            await _db.Child("users").Child(uid).Child("preferences").PutAsync(new { plant, region });

            var docRef = _firestore.Collection("users").Document(uid)
                                   .Collection("preferences").Document("settings");

            await docRef.SetAsync(new Dictionary<string, object>
            {
                { "plant", plant },
                { "region", region },
                { "updatedAt", Timestamp.GetCurrentTimestamp() }
            });
        }

        // ------------------- 🔹 DATOS EXTRA -------------------
        public async Task<Dictionary<string, object>> GetUserExtraDataAsync(string uid)
        {
            var docRef = _firestore.Collection("users").Document(uid);
            var snapshot = await docRef.GetSnapshotAsync();
            return snapshot.Exists ? snapshot.ToDictionary() : new Dictionary<string, object>();
        }

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
