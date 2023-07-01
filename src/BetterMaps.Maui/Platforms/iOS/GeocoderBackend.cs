using Contacts;
using CoreLocation;
using CCLGeocoder = CoreLocation.CLGeocoder;

namespace BetterMaps.Maui.iOS
{
    public class GeocoderBackend : IGeocoder
    {
        private readonly CCLGeocoder _geocoder;

        public GeocoderBackend()
        {
            _geocoder = new CCLGeocoder();
        }

        public Task<IEnumerable<string>> GetAddressesForPositionAsync(Position position)
        {
            var location = new CLLocation(position.Latitude, position.Longitude);
            var source = new TaskCompletionSource<IEnumerable<string>>();

            _geocoder.ReverseGeocodeLocation(location, (placemarks, error) =>
            {
                var addresses = placemarks
                    ?.Select(p => CNPostalAddressFormatter.GetStringFrom(p.PostalAddress, CNPostalAddressFormatterStyle.MailingAddress))
                    ?? Enumerable.Empty<string>();
                source.SetResult(addresses);
            });

            return source.Task;
        }

        public Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address)
        {
            var source = new TaskCompletionSource<IEnumerable<Position>>();

            _geocoder.GeocodeAddress(address, (placemarks, error) =>
            {
                var positions = placemarks
                    ?.Select(p => new Position(p.Location.Coordinate.Latitude, p.Location.Coordinate.Longitude))
                    ?? Enumerable.Empty<Position>();
                source.SetResult(positions);
            });

            return source.Task;
        }
    }
}