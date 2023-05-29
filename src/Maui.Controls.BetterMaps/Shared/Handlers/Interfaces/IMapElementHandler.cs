#if IOS || MACCATALYST
using PlatformView = MapKit.IMKOverlay;
#elif ANDROID
using PlatformView = Maui.Controls.BetterMaps.Android.IMauiMapElement;
#elif NETSTANDARD || (NET6_0 && !IOS && !ANDROID)
using PlatformView = System.Object;
#endif

namespace Maui.Controls.BetterMaps.Handlers
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