namespace BetterMaps.Maui
{
    public interface IMapElement : IElement, IStroke
    {
        object MapElementId { get; set; }
    }
}
