#if IOS || MACCATALYST
using PlatformView = MapKit.IMKAnnotation;
#elif ANDROID
using PlatformView = BetterMaps.Maui.Android.MauiMapMarker;
#elif NETSTANDARD || (NET6_0 && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace BetterMaps.Maui.Handlers
{
    public partial class MapPinHandler : IMapPinHandler
    {
        public static IPropertyMapper<IMapPin, IMapPinHandler> Mapper = new PropertyMapper<IMapPin, IMapPinHandler>(ElementMapper)
        {
            [nameof(IMapPin.Label)] = MapLabel,
            [nameof(IMapPin.Address)] = MapAddress,
            [nameof(IMapPin.Position)] = MapPosition,
            [nameof(IMapPin.Anchor)] = MapAnchor,
            [nameof(IMapPin.ZIndex)] = MapZIndex,
            [nameof(IMapPin.CanShowInfoWindow)] = MapCanShowInfoWindow,
            [nameof(IMapPin.TintColor)] = MapTintColor,
            [nameof(IMapPin.ImageSource)] = MapImageSource,
        };

        public MapPinHandler() : base(Mapper)
        {

        }

        public MapPinHandler(IPropertyMapper mapper = null)
            : base(mapper ?? Mapper)
        {
        }

        IMapPin IMapPinHandler.VirtualView => VirtualView;

        PlatformView IMapPinHandler.PlatformView => PlatformView;

#if IOS || MACCATALYST
#elif ANDROID
#endif
    }
}
