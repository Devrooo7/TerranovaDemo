using Microsoft.Maui.Controls;
using System.Threading.Tasks;
using TerranovaDemo.Services;

namespace TerranovaDemo.Pages
{
    public partial class SplashPage : ContentPage
    {
        public SplashPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            await Task.Delay(1500);

            string uid = SessionStore.GetUid();
            string name = SessionStore.GetUserName();

            AppState.CurrentUserUid = uid;
            AppState.CurrentUserName = name;

            Application.Current.MainPage = new AppShell();

            if (!string.IsNullOrWhiteSpace(uid))
            {
                // Usuario autenticado
                await Shell.Current.GoToAsync("//mainpage");
            }
            else
            {
                // Ir a login
                await Shell.Current.GoToAsync("//loginpage");
            }
        }
    }
}
