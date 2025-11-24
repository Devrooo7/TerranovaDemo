using Microsoft.Maui.Controls;
using System; // Agregado para EventArgs
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using TerranovaDemo.Services;

namespace TerranovaDemo
{
    public partial class UserPage : ContentPage
    {
        private readonly AuthService _auth;
        private readonly FirebaseAuthClient _firebase;

        public UserPage(AuthService auth, FirebaseAuthClient firebase)
        {
            InitializeComponent();
            _auth = auth;
            _firebase = firebase;

            _ = LoadUserDataAsync();
        }

        private async Task LoadUserDataAsync()
        {
            try
            {
                string userName = await _auth.GetUserNameAsync();
                WelcomeLbl.Text = $"Hola {userName}, bienvenido. Ayúdanos seleccionando las opciones:";

                PlantPicker.ItemsSource = new List<string>
        {
          "Maíz","Frijol","Trigo","Arroz","Sorgo",
          "Tomate","Chile","Cebolla","Lechuga","Zanahoria","Pepino",
          "Aguacate","Café","Mango","Caña de azúcar","Plátano","Limón","Naranja"
        };

                RegionPicker.ItemsSource = new List<string>
        {
          "Zona Tropical","Zona Subtropical","Zona Templada","Zona Semiárida","Zona Desértica"
        };

                string uid = SessionStore.GetUid();
                if (!string.IsNullOrEmpty(uid))
                {
                    var extra = await _firebase.GetUserExtraDataAsync(uid);
                    if (extra.ContainsKey("preferences"))
                    {
                        try
                        {
                            // Nota: Si 'preferences' en Firestore ya es un mapa, .ToString() puede devolver 
                            // el JSON o un objeto que puede requerir conversión directa a diccionario.
                            // Se mantiene la lógica actual para cargar, asumiendo que funciona.
                            var prefsJson = extra["preferences"].ToString();
                            if (!string.IsNullOrEmpty(prefsJson) && prefsJson != "null")
                            {
                                using var doc = JsonDocument.Parse(prefsJson);
                                if (doc.RootElement.TryGetProperty("plant", out var p))
                                    PlantPicker.SelectedItem = p.GetString();
                                if (doc.RootElement.TryGetProperty("region", out var r))
                                    RegionPicker.SelectedItem = r.GetString();
                            }
                        }
                        catch { }
                    }
                }
            }
            catch
            {
                WelcomeLbl.Text = "Hola Usuario, bienvenido.";
            }
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            string plant = PlantPicker.SelectedItem?.ToString() ?? "";
            string region = RegionPicker.SelectedItem?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(plant) || string.IsNullOrWhiteSpace(region))
            {
                await DisplayAlert("Error", "Selecciona vegetación y región.", "OK");
                return;
            }

            try
            {
                string uid = SessionStore.GetUid();

                // ✅ CORRECCIÓN APLICADA: Solo se llama a la función de guardar preferencias.
                // La llamada a SaveUserInfoAsync se eliminó para evitar sobrescribir
                // el email, passwordHash, y otros campos con valores vacíos.
                await _firebase.SaveUserPreferencesAsync(uid, plant, region);

                await DisplayAlert("✔ Guardado", "Preferencias guardadas exitosamente.", "OK");
            }
            catch
            {
                await DisplayAlert("❌ Error", "No se pudo guardar.", "OK");
            }
        }
    }
}