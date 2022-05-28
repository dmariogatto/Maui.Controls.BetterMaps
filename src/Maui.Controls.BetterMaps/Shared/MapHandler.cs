#if IOS || MACCATALYST
using PlatformView = Maui.Controls.BetterMaps.iOS.MauiMapView;
#elif ANDROID
using PlatformView = Maui.Controls.BetterMaps.Android.MauiMapView;
#elif NETSTANDARD || (NET6_0 && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace Maui.Controls.BetterMaps
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
        };

        public MapHandler() : this(Mapper)
        {
        }

        public MapHandler(IPropertyMapper mapper) : base(mapper)
        {
        }

        IMap IMapHandler.VirtualView => VirtualView;

        PlatformView IMapHandler.PlatformView => PlatformView;

#if IOS || MACCATALYST
#elif ANDROID
#endif
    }
}
