using AGeocoder = Android.Locations.Geocoder;

namespace Maui.Controls.BetterMaps.Android
{
    public class GeocoderBackend : IGeocoder
    {
        private readonly AGeocoder _geocoder;

        public GeocoderBackend()
        {
            _geocoder = new AGeocoder(Platform.CurrentActivity);
        }

        public async Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address)
        {
            var addresses = await _geocoder.GetFromLocationNameAsync(address, 5);
            return addresses?.Select(p => new Position(p.Latitude, p.Longitude)) ?? Enumerable.Empty<Position>();
        }

        public async Task<IEnumerable<string>> GetAddressesForPositionAsync(Position position)
        {
            var addresses = await _geocoder.GetFromLocationAsync(position.Latitude, position.Longitude, 5);
            return addresses?.Select(p =>
            {
                var lines = Enumerable.Range(0, p.MaxAddressLineIndex + 1).Select(p.GetAddressLine);
                return string.Join(Environment.NewLine, lines);
            }) ?? Enumerable.Empty<string>();
        }
    }
}
