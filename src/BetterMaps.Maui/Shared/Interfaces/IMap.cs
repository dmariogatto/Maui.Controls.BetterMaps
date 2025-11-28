using System.Collections.ObjectModel;
using System.ComponentModel;

namespace BetterMaps.Maui
{
    public interface IMap : IView, INotifyPropertyChanged
    {
        bool HasScrollEnabled { get; }
        bool HasZoomEnabled { get; }
        bool IsShowingUser { get; }

        MapSpan LastMoveToRegion { get; }

        MapTheme MapTheme { get; }
        MapType MapType { get; }

        bool MoveToLastRegionOnLayoutChange { get; }

        Pin SelectedPin { get; set; }
        bool ShowCompass { get; }
        bool ShowUserLocationButton { get; }
        bool TrafficEnabled { get; }
        MapSpan VisibleRegion { get; }

        Thickness LayoutMargin { get; }

        ObservableCollection<Pin> Pins { get; }
        ObservableCollection<MapElement> MapElements { get; }

        void MoveToRegion(MapSpan mapSpan);

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool CanSendMapClicked();
        [EditorBrowsable(EditorBrowsableState.Never)]
        void SendMapClicked(Position position);

        [EditorBrowsable(EditorBrowsableState.Never)]
        bool CanSendMapLongClicked();
        [EditorBrowsable(EditorBrowsableState.Never)]
        void SendMapLongClicked(Position position);

        [EditorBrowsable(EditorBrowsableState.Never)]
        void SendPinClick(Pin pin);

        [EditorBrowsable(EditorBrowsableState.Never)]
        void SendInfoWindowClick(Pin pin);

        [EditorBrowsable(EditorBrowsableState.Never)]
        void SendInfoWindowLongClick(Pin pin);

        [EditorBrowsable(EditorBrowsableState.Never)]
        void SetVisibleRegion(MapSpan value);
    }
}