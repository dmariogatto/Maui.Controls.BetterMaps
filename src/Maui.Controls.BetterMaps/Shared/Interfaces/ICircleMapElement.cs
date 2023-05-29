namespace Maui.Controls.BetterMaps
{
    public interface ICircleMapElement : IMapElement, IFilledMapElement
	{
        Position Center { get; }
        Distance Radius { get; }
	}
}
