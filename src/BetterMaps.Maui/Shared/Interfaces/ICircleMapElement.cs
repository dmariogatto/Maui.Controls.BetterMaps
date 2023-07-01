namespace BetterMaps.Maui
{
    public interface ICircleMapElement : IMapElement, IFilledMapElement
    {
        Position Center { get; }
        Distance Radius { get; }
    }
}
