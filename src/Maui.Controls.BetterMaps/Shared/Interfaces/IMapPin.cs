using System.ComponentModel;

namespace Maui.Controls.BetterMaps
{
    public interface IMapPin : IElement
    {
        string Label { get; }
        string Address { get; }
        Position Position { get; }
        Point Anchor { get; }
        int ZIndex { get; }

        bool CanShowInfoWindow { get; }

        Color TintColor { get; }

        ImageSource ImageSource { get; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        CancellationTokenSource ImageSourceCts { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        object NativeId { get; set; }
    }
}