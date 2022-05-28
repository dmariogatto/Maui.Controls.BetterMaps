using CoreLocation;
using Maui.Controls.BetterMaps;
using Maui.Controls.BetterMaps.iOS;
using UIKit;

namespace Maui
{
    public static class MauiBetterMaps
    {
        private static bool _initialized;

        private static bool? _isiOs13OrNewer;
        private static bool? _isiOs14OrNewer;

        private static CLLocationManager _locationManager;

        public static IMapCache Cache { get; private set; }

        internal static bool Ios13OrNewer
            => _isiOs13OrNewer ??= UIDevice.CurrentDevice.CheckSystemVersion(13, 0);

        internal static bool Ios14OrNewer
            => _isiOs14OrNewer ??= UIDevice.CurrentDevice.CheckSystemVersion(14, 0);

        internal static CLLocationManager LocationManager
            => _locationManager ??= new CLLocationManager();

        public static void Init(IMapCache mapCache = null)
		{
			if (_initialized)
				return;
			GeocoderBackend.Register();
            _initialized = true;
            Cache = mapCache;
        }
	}
}