#if IOS || MACCATALYST
using PlatformView = MapKit.IMKOverlay;
#elif ANDROID
using PlatformView = BetterMaps.Maui.Android.IMauiMapElement;
#elif NETSTANDARD || (NET6_0 && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace BetterMaps.Maui.Handlers
{
    public partial interface IMapElementHandler : IElementHandler
    {
        new IMapElement VirtualView { get; }
        new PlatformView PlatformView { get; }

#if IOS || MACCATALYST
#elif ANDROID
#endif
    }
}