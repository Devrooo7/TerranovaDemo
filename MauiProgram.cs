using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using CommunityToolkit.Maui;
using SkiaSharp.Views.Maui.Controls.Hosting;
using TerranovaDemo.Services;
using Microsoft.Extensions.DependencyInjection;

namespace TerranovaDemo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // ---------------------------------------------------------
        // FIREBASE CLIENT: usa argumentos posicionales (constructor existente)
        // Reemplaza API key / URL si quieres otro.
        // ---------------------------------------------------------
        builder.Services.AddSingleton<FirebaseAuthClient>(sp =>
            new FirebaseAuthClient(
                "AIzaSyAEQNaGohNG32f52DZPbgljT9Rz3w6O-bM",
                "https://terranova-62f60-default-rtdb.firebaseio.com/"
            )
        );

        // AuthService requiere FirebaseAuthClient (constructor inyectado abajo)
        builder.Services.AddSingleton<AuthService>();

        // Páginas registradas para DI (usar AddTransient o AddSingleton según preferencia)
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<UserPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddSingleton<AppShell>();

        Environment.SetEnvironmentVariable("SKIA_SHARP_DISABLE_GPU", "1");

        return builder.Build();
    }
}
