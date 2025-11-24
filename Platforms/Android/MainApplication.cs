using Android.App;
using Android.Runtime;
using Microsoft.Maui;
using System;
using System.IO;
using Microsoft.Maui.Storage;

namespace TerranovaDemo
{
    [Application]
    public class MainApplication : MauiApplication
    {
        public MainApplication(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        protected override MauiApp CreateMauiApp()
        {
            CopyServiceAccountFile();
            return MauiProgram.CreateMauiApp();
        }

        private void CopyServiceAccountFile()
        {
            try
            {
                string dest = Path.Combine(FileSystem.AppDataDirectory, "service_account.json");

                if (!File.Exists(dest))
                {
                    using var input = Android.App.Application.Context.Resources
                        .OpenRawResource(Resource.Raw.service_account);

                    using var output = File.Create(dest);
                    input.CopyTo(output);
                }

                Environment.SetEnvironmentVariable(
                    "GOOGLE_APPLICATION_CREDENTIALS",
                    dest
                );

                System.Diagnostics.Debug.WriteLine("✅ service_account.json copiado y registrado.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error copiando service_account.json: {ex}");
            }
        }
    }
}
