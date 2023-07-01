#if IOS || MACCATALYST
using PlatformView = MapKit.IMKOverlay;
#elif ANDROID
using PlatformView = BetterMaps.Maui.Android.IMauiMapElement;
#elif NETSTANDARD || (NET6_0 && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace BetterMaps.Maui.Handlers
{
    public partial class MapElementHandler : IMapElementHandler
    {
        public static IPropertyMapper<IMapElement, IMapElementHandler> Mapper = new PropertyMapper<IMapElement, IMapElementHandler>(ElementMapper)
        {
            [nameof(IMapElement.Stroke)] = MapStroke,
            [nameof(IMapElement.StrokeThickness)] = MapStrokeThickness,
            [nameof(IFilledMapElement.Fill)] = MapFill,
            [nameof(GeopathElement.Geopath)] = MapGeopath,
            [nameof(ICircleMapElement.Radius)] = MapRadius,
            [nameof(ICircleMapElement.Center)] = MapCenter,
        };

        public MapElementHandler() : base(Mapper)
        {

        }

        public MapElementHandler(IPropertyMapper mapper = null)
            : base(mapper ?? Mapper)
        {
        }

        IMapElement IMapElementHandler.VirtualView => VirtualView;

        PlatformView IMapElementHandler.PlatformView => PlatformView;

#if IOS || MACCATALYST
#elif ANDROID
#endif
    }
}
