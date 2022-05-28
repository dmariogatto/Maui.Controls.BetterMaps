using Foundation;

namespace Maui.Controls.BetterMaps.Sample
{
    [Register("AppDelegate")]
    public class AppDelegate : MauiUIApplicationDelegate
    {
        protected override MauiApp CreateMauiApp()
        {
            MauiBetterMaps.Init();
            return MauiProgram.CreateMauiApp();
        }
    }
}