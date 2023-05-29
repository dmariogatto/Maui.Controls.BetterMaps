using CoreLocation;
using Maui.Controls.BetterMaps.iOS;

namespace Maui
{
    public static class MauiBetterMaps
    {
        public static bool IsInitialized { get; private set; }

        private static CLLocationManager _locationManager;
        internal static CLLocationManager LocationManager => _locationManager ??= new CLLocationManager();

        public static void Init()
		{
			if (IsInitialized)
				return;

            IsInitialized = true;
        }
	}
}