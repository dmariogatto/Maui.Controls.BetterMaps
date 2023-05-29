using CoreLocation;
using Maui.Controls.BetterMaps;
using Maui.Controls.BetterMaps.iOS;

namespace Maui
{
    public static class MauiBetterMaps
    {
        private static bool _initialized;

        public static IMapCache Cache { get; private set; }

        private static CLLocationManager _locationManager;
        internal static CLLocationManager LocationManager => _locationManager ??= new CLLocationManager();

        public static void Init(IMapCache mapCache = null)
		{
			if (_initialized)
				return;

			_initialized = true;

            Cache = mapCache;

            GeocoderBackend.Register();
        }
	}
}