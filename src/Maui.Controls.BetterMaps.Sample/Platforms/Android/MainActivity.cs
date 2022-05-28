using Android.App;
using Android.Content.PM;
using Android.OS;

namespace Maui.Controls.BetterMaps.Sample
{
    [Activity(
        Theme = "@style/Maui.SplashTheme",
        MainLauncher = true,
        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            MauiBetterMaps.Init(this, savedInstanceState);
            MauiBetterMaps.SetLightThemeAsset("map.style.light.json");
            MauiBetterMaps.SetDarkThemeAsset("map.style.dark.json");
        }
    }
}