namespace Maui.Controls.BetterMaps
{
	public interface IMapElement : IElement, IStroke
	{
		object MapElementId { get; set; }
	}
}
