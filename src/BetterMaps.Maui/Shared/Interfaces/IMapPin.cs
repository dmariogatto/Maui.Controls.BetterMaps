using System.ComponentModel;

namespace BetterMaps.Maui
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
        object NativeId { get; set; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void SetImageCts(CancellationTokenSource cancellationTokenSource);

        [EditorBrowsable(EditorBrowsableState.Never)]
        public void CancelImageCts();
    }
}