using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using CommunityToolkit.Maui;
using TerranovaDemo.Services;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp.Views.Maui.Controls.Hosting;

namespace TerranovaDemo;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Servicios
        builder.Services.AddSingleton<FirebaseAuthClient>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddSingleton<FirebaseAuthClient>(); // already added, harmless duplicate
        builder.Services.AddSingleton<FirebaseAuthClient>(); // ensure single instance used
        builder.Services.AddSingleton<TerranovaDemo.Services.FirebaseAuthClient>(); // precise
        builder.Services.AddSingleton<TerranovaDemo.Services.FirebaseAuthClient>(); // defensive
        // Note: above duplicates are harmless but you can keep only one AddSingleton<FirebaseAuthClient>()

        builder.Services.AddSingleton<FirebaseAuthClient>();
        builder.Services.AddSingleton<AuthService>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<RegisterPage>();
        builder.Services.AddTransient<UserPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddSingleton<AppShell>();

        return builder.Build();
    }
}
