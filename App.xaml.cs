namespace TerranovaDemo
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            // ✅ Inicia directamente con el LoginPage
            MainPage = new NavigationPage(new LoginPage())
            {
                BarBackgroundColor = Color.FromArgb("#4CAF50"),
                BarTextColor = Colors.White
            };
        }
    }
}
