#if IOS || MACCATALYST
using PlatformView = BetterMaps.Maui.iOS.MauiMapView;
#elif ANDROID
using PlatformView = BetterMaps.Maui.Android.MauiMapView;
#elif NETSTANDARD || (NET6_0 && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace BetterMaps.Maui.Handlers
{
    public partial interface IMapHandler : IViewHandler
    {
        new IMap VirtualView { get; }
        new PlatformView PlatformView { get; }

#if IOS || MACCATALYST
#elif ANDROID
#endif
    }
}