using CoreGraphics;
using MapKit;
using UIKit;

namespace Maui.Controls.BetterMaps.iOS
{
    internal static class MauiMapViewExtensions
    {
        internal static void UpdateTheme(this MauiMapView map, MapTheme mapTheme)
        {
            if (map is null || !OperatingSystem.IsIOSVersionAtLeast(13))
                return;

            map.OverrideUserInterfaceStyle = mapTheme switch
            {
                MapTheme.System => UIUserInterfaceStyle.Unspecified,
                MapTheme.Light => UIUserInterfaceStyle.Light,
                MapTheme.Dark => UIUserInterfaceStyle.Dark,
                _ => throw new NotSupportedException($"Unknown map theme '{mapTheme}'")
            };
        }

        internal static void UpdateType(this MauiMapView map, MapType type)
        {
            if (map is null)
                return;

            map.MapType = type switch
            {
                MapType.Street => MKMapType.MutedStandard,
                MapType.Satellite => MKMapType.Satellite,
                MapType.Hybrid => MKMapType.Hybrid,
                _ => throw new NotSupportedException($"Unknown map type '{type}'")
            };

            if (OperatingSystem.IsIOSVersionAtLeast(13))
            {
                map.PointOfInterestFilter = new MKPointOfInterestFilter(Array.Empty<MKPointOfInterestCategory>());
            }
            else
            {
                map.ShowsPointsOfInterest = false;
            }
        }

        internal static void UpdateIsShowingUser(this MauiMapView map, bool isShowingUser)
        {
            if (map is null)
                return;

            if (isShowingUser)
                map.LocationManager.RequestWhenInUseAuthorization();

            map.ShowsUserLocation = isShowingUser;
        }

        internal static void UpdateShowUserLocationButton(this MauiMapView map, bool showUserLocationButton, MKUserTrackingButton userTrackingButton)
        {
            if (map is null || !showUserLocationButton)
                return;

            const float utSize = 48f;
            userTrackingButton.Layer.CornerRadius = utSize / 2;
            userTrackingButton.Layer.BorderWidth = 0.25f;

            var circleMask = new CoreAnimation.CAShapeLayer();
            var circlePath = UIBezierPath.FromRoundedRect(new CGRect(0, 0, utSize, utSize), utSize / 2);
            circleMask.Path = circlePath.CGPath;
            userTrackingButton.Layer.Mask = circleMask;

            userTrackingButton.TranslatesAutoresizingMaskIntoConstraints = false;

            map.AddSubview(userTrackingButton);

            var margins = map.LayoutMarginsGuide;
            NSLayoutConstraint.ActivateConstraints(new[]
            {
                userTrackingButton.BottomAnchor.ConstraintEqualTo(margins.BottomAnchor, -46),
                userTrackingButton.TrailingAnchor.ConstraintEqualTo(margins.TrailingAnchor, -12),
                userTrackingButton.WidthAnchor.ConstraintEqualTo(utSize),
                userTrackingButton.HeightAnchor.ConstraintEqualTo(userTrackingButton.WidthAnchor),
            });
        }

        internal static void UpdateShowCompass(this MauiMapView map, bool showCompass)
        {
            if (map is null)
                return;
            map.ShowsCompass = showCompass;
        }

        internal static void UpdateHasScrollEnabled(this MauiMapView map, bool hasScrollEnabled)
        {
            if (map is null)
                return;
            map.ScrollEnabled = hasScrollEnabled;
        }

        internal static void UpdateHasZoomEnabled(this MauiMapView map, bool hasZoomEnabled)
        {
            if (map is null)
                return;
            map.ZoomEnabled = hasZoomEnabled;
        }

        internal static void UpdateTrafficEnabled(this MauiMapView map, bool trafficEnabled)
        {
            if (map is null)
                return;
            map.ShowsTraffic = trafficEnabled;
        }
    }
}
