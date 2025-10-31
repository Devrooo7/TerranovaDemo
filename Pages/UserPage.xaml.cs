using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using TerranovaDemo.Services;
using System.Threading.Tasks;

namespace TerranovaDemo
{
    public partial class UserPage : ContentPage
    {
        public UserPage()
        {
            InitializeComponent();
            LoadUserDataAsync();
        }

        private async Task LoadUserDataAsync()
        {
            try
            {
                var authClient = new FirebaseAuthClient();
                string currentUserId = AppState.CurrentUserUid;

                var userExtraData = await authClient.GetUserExtraDataAsync(currentUserId);

                string nombreUsuario = userExtraData.ContainsKey("name")
                                       ? userExtraData["name"].ToString()!
                                       : AppState.CurrentUserName;

                WelcomeLbl.Text = $"Hola {nombreUsuario}, bienvenido. Ayúdanos seleccionando las opciones:";

                PlantPicker.ItemsSource = new List<string> { "Cactus", "Orquídea", "Helecho" };
                RegionPicker.ItemsSource = new List<string> { "Tropical", "Desértica", "Templada" };
            }
            catch
            {
                WelcomeLbl.Text = "Hola Usuario, bienvenido. Ayúdanos seleccionando las opciones:";
            }
        }

        private async void SaveButton_Clicked(object sender, EventArgs e)
        {
            string plant = PlantPicker.SelectedItem?.ToString() ?? "";
            string region = RegionPicker.SelectedItem?.ToString() ?? "";

            if (string.IsNullOrWhiteSpace(plant) || string.IsNullOrWhiteSpace(region))
            {
                await DisplayAlert("Error", "Selecciona tanto la vegetación como la región.", "OK");
                return;
            }

            try
            {
                var authClient = new FirebaseAuthClient();
                string currentUserId = AppState.CurrentUserUid;
                await authClient.SaveUserPreferencesAsync(currentUserId, plant, region);

                await DisplayAlert("✅ Guardado", "Preferencias guardadas correctamente.", "OK");
            }
            catch
            {
                await DisplayAlert("❌ Error", "No se pudo guardar las preferencias.", "OK");
            }
        }
    }
}
