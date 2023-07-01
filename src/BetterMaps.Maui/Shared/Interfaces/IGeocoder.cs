namespace BetterMaps.Maui
{
    public interface IGeocoder
    {
        Task<IEnumerable<string>> GetAddressesForPositionAsync(Position position);
        Task<IEnumerable<Position>> GetPositionsForAddressAsync(string address);
    }
}