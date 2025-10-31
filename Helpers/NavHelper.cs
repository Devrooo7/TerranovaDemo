using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace TerranovaDemo.Helpers
{
    public static class NavHelper
    {
        public static Task ResetToLoginAsync()
        {
            return MainThread.InvokeOnMainThreadAsync(() =>
            {
                var loginRoot = new NavigationPage(new LoginPage());
                var app = Application.Current;

                var win = app?.Windows?.FirstOrDefault();
                if (win is not null)
                {
                    try { win.Page = loginRoot; return; } catch { }
                }

#pragma warning disable CS0618
                try { app!.MainPage = loginRoot; return; } catch { }
#pragma warning restore CS0618

                try { app?.OpenWindow(new Window(loginRoot)); } catch { }
            });
        }

        public static Task ResetToShellAsync()
        {
            return MainThread.InvokeOnMainThreadAsync(() =>
            {
                var shell = new AppShell();
                var app = Application.Current;

                var win = app?.Windows?.FirstOrDefault();
                if (win is not null)
                {
                    try { win.Page = shell; return; } catch { }
                }

#pragma warning disable CS0618
                try { app!.MainPage = shell; return; } catch { }
#pragma warning restore CS0618

                try { app?.OpenWindow(new Window(shell)); } catch { }
            });
        }
    }
}
