#if IOS || MACCATALYST
using PlatformView = BetterMaps.Maui.iOS.MauiMapView;
#elif ANDROID
using Android.Gms.Maps;
using PlatformView = Android.Widget.FrameLayout;
#elif NETSTANDARD || (NET6_0 && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace BetterMaps.Maui.Handlers
{
    public partial class MapHandler : IMapHandler
    {
        public static IPropertyMapper<IMap, IMapHandler> Mapper = new PropertyMapper<IMap, IMapHandler>(ViewMapper)
        {
            [nameof(IMap.MapTheme)] = MapMapTheme,
            [nameof(IMap.MapType)] = MapMapType,
            [nameof(IMap.IsShowingUser)] = MapIsShowingUser,
            [nameof(IMap.ShowUserLocationButton)] = MapShowUserLocationButton,
            [nameof(IMap.ShowCompass)] = MapShowCompass,
            [nameof(IMap.HasScrollEnabled)] = MapHasScrollEnabled,
            [nameof(IMap.HasZoomEnabled)] = MapHasZoomEnabled,
            [nameof(IMap.TrafficEnabled)] = MapTrafficEnabled,
            [nameof(IMap.SelectedPin)] = MapSelectedPin,
            [nameof(IMap.Height)] = MapHeight,
        };

        public static CommandMapper<IMap, IMapHandler> CommandMapper = new(ViewCommandMapper)
        {
            [nameof(IMap.MoveToRegion)] = MapMoveToRegion,
#if  ANDROID
            [nameof(MapView.ViewAttachedToWindow)] = MapViewAttachedToWindow,
#endif
        };

        public MapHandler() : base(Mapper, CommandMapper)
        {
        }

        public MapHandler(IPropertyMapper mapper, CommandMapper commandMapper) : base(mapper, commandMapper)
        {
        }

        IMap IMapHandler.VirtualView => VirtualView;

        PlatformView IMapHandler.PlatformView => PlatformView;

#if IOS || MACCATALYST
#elif ANDROID
#endif
    }
}
