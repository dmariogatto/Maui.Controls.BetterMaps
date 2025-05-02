using Android.App;
using Android.Gms.Common;
using Android.Gms.Maps;
using Android.OS;
using BetterMaps.Maui;
using BetterMaps.Maui.Android;
using BetterMaps.Maui.Handlers;

namespace BetterMaps
{
    public static class ActivityExtensions
    {
        private static bool _initialized;

        internal static readonly Dictionary<MapTheme, string> MapThemeAssetNames = new Dictionary<MapTheme, string>();

        public static void GoogleMapsSdkInit(this Activity activity)
            => GoogleMapsSdkInit(activity, null, null, null);

        public static void GoogleMapsSdkInit(
            this Activity activity,
            Action<MapsInitializer.Renderer> onGoogleMapsSdkInitialized,
            string lightThemeAsset,
            string darkThemeAsset)
        {
            if (_initialized)
                return;

            _initialized = true;

            if (!string.IsNullOrWhiteSpace(lightThemeAsset))
                MapThemeAssetNames[MapTheme.Light] = lightThemeAsset;
            if (!string.IsNullOrWhiteSpace(darkThemeAsset))
                MapThemeAssetNames[MapTheme.Dark] = darkThemeAsset;

#pragma warning disable 618
            if (GooglePlayServicesUtil.IsGooglePlayServicesAvailable(activity) == ConnectionResult.Success)
#pragma warning restore 618
            {
                try
                {
                    var rendererCallback = default(OnMapsSdkInitializedCallback);
                    if (onGoogleMapsSdkInitialized is not null)
                    {
                        void onMapsSdkInitialized(object sender, OnGoogleMapsSdkInitializedEventArgs args)
                        {
                            onGoogleMapsSdkInitialized?.Invoke(args.Renderer);

                            if (rendererCallback is not null)
                            {
                                rendererCallback.OnGoogleMapsSdkInitialized -= onMapsSdkInitialized;
                                rendererCallback.Dispose();
                                rendererCallback = null;
                            }
                        }

                        rendererCallback = new OnMapsSdkInitializedCallback();
                        rendererCallback.OnGoogleMapsSdkInitialized += onMapsSdkInitialized;
                    }

                    MapsInitializer.Initialize(activity, MapsInitializer.Renderer.Latest, rendererCallback);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Google Play Services Not Found");
                    Console.WriteLine("Exception: {0}", e);
                }
            }
        }
    }
}
