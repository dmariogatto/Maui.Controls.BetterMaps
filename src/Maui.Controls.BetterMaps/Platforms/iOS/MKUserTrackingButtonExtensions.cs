using MapKit;
using UIKit;

namespace Maui.Controls.BetterMaps.iOS
{
    internal static class MKUserTrackingButtonExtensions
    {
        internal static void UpdateTheme(this MKUserTrackingButton trackingButton, bool isDarkMode)
        {
            if (trackingButton is null)
                return;

            trackingButton.Layer.BackgroundColor = (isDarkMode ? UIColor.FromRGBA(49, 49, 51, 230) : UIColor.FromRGBA(255, 255, 255, 230)).CGColor;
            trackingButton.Layer.BorderColor = (isDarkMode ? UIColor.FromRGBA(0, 0, 0, 230) : UIColor.FromRGBA(191, 191, 191, 230)).CGColor;
        }
    }
}
