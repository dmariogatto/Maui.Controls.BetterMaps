#if IOS || MACCATALYST
using PlatformView = Maui.Controls.BetterMaps.iOS.MauiMapView;
#elif ANDROID
using PlatformView = Maui.Controls.BetterMaps.Android.MauiMapView;
#elif NETSTANDARD || (NET6_0 && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace Maui.Controls.BetterMaps.Handlers
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