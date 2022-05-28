using System.ComponentModel;

namespace Maui.Controls.BetterMaps
{
    public class Geocoder
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Func<string, Task<IEnumerable<Position>>> GetPositionsForAddressAsyncFunc;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static Func<Position, Task<IEnumerable<string>>> GetAddressesForPositionFuncAsync;

        public Task<IEnumerable<string>> GetAddressesForPositionAsync(Position position)
        {
            if (GetAddressesForPositionFuncAsync == null)
                throw new InvalidOperationException("You MUST call Xamarin.FormsBetterMaps.Init (); prior to using it.");
            return GetAddressesForPositionFuncAsync(position);
        }

        public Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address)
        {
            if (GetPositionsForAddressAsyncFunc == null)
                throw new InvalidOperationException("You MUST call Xamarin.FormsBetterMaps.Init (); prior to using it.");
            return GetPositionsForAddressAsyncFunc(address);
        }
    }
}