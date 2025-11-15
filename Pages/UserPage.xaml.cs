using Microsoft.Maui.Controls;
using System.Collections.Generic;
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

                PlantPicker.ItemsSource = new List<string> { "Cactus", "Orquídea", "Helecho" };
                RegionPicker.ItemsSource = new List<string> { "Tropical", "Desértica", "Templada" };
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
