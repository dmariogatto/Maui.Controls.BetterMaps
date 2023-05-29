using Android.App;
using Android.Gms.Common;
using Android.Gms.Maps;
using Android.OS;
using Maui.Controls.BetterMaps;
using Maui.Controls.BetterMaps.Android;
using Maui.Controls.BetterMaps.Handlers;

namespace Maui
{
    public static class MauiBetterMaps
	{
		internal static readonly Dictionary<MapTheme, string> AssetFileNames = new Dictionary<MapTheme, string>();

		public static bool IsInitialized { get; private set; }
		public static IMapCache Cache { get; private set; }

		public static void Init(Activity activity, Bundle bundle, IMapCache mapCache)
		    => Init(activity, bundle, GoogleMapsRenderer.Latest, null, mapCache);

        public static void Init(Activity activity, Bundle bundle, GoogleMapsRenderer renderer, Action<MapsInitializer.Renderer> onGoogleMapsSdkInitialized, IMapCache mapCache)
        {
            if (IsInitialized)
                return;

            IsInitialized = true;
            Cache = mapCache;

            MapHandler.Bundle = bundle;

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

                    _ = renderer switch
                    {
                        GoogleMapsRenderer.Legacy => MapsInitializer.Initialize(activity, MapsInitializer.Renderer.Legacy, rendererCallback),
                        _ => MapsInitializer.Initialize(activity, MapsInitializer.Renderer.Latest, rendererCallback),
                    };
                }
                catch (Exception e)
                {
                    Console.WriteLine("Google Play Services Not Found");
                    Console.WriteLine("Exception: {0}", e);
                }
            }

            GeocoderBackend.Register(activity);
        }

        public static void SetLightThemeAsset(string assetFileName)
			=> AssetFileNames[MapTheme.Light] = assetFileName;

		public static void SetDarkThemeAsset(string assetFileName)
			=> AssetFileNames[MapTheme.Dark] = assetFileName;
    }
}
