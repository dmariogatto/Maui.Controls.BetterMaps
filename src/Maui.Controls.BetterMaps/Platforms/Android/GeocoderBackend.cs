using Android.Content;
using AGeocoder = Android.Locations.Geocoder;

namespace Maui.Controls.BetterMaps.Android
{
    internal class GeocoderBackend
	{
		private readonly Context _context;

        private AGeocoder _geocoder;
        private AGeocoder AndroidGeocoder => _geocoder ??= new AGeocoder(_context);

		public GeocoderBackend(Context context)
		{
			_context = context;
		}

		public void Register()
		{
			Geocoder.GetPositionsForAddressAsyncFunc = GetPositionsForAddressAsync;
			Geocoder.GetAddressesForPositionFuncAsync = GetAddressesForPositionAsync;
		}

		public async Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address)
		{
			var addresses = await AndroidGeocoder.GetFromLocationNameAsync(address, 5);
			return addresses.Select(p => new Position(p.Latitude, p.Longitude));
		}

		public async Task<IEnumerable<string>> GetAddressesForPositionAsync(Position position)
		{
			var addresses = await AndroidGeocoder.GetFromLocationAsync(position.Latitude, position.Longitude, 5);
			return addresses.Select(p =>
			{
				IEnumerable<string> lines = Enumerable.Range(0, p.MaxAddressLineIndex + 1).Select(p.GetAddressLine);
				return string.Join("\n", lines);
			});
		}
	}
}