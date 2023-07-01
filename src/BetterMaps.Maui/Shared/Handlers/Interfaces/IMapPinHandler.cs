#if IOS || MACCATALYST
using PlatformView = MapKit.IMKAnnotation;
#elif ANDROID
using PlatformView = BetterMaps.Maui.Android.MauiMapMarker;
#elif NETSTANDARD || (NET6_0 && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace BetterMaps.Maui.Handlers
{
    public partial interface IMapPinHandler : IElementHandler
    {
        new IMapPin VirtualView { get; }
        new PlatformView PlatformView { get; }

#if IOS || MACCATALYST
#elif ANDROID
#endif
    }
}