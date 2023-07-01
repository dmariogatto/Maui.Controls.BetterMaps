using CoreGraphics;
using MapKit;
using UIKit;

namespace BetterMaps.Maui.iOS
{
    internal static class MauiMapViewExtensions
    {
        internal static void UpdateTheme(this MauiMapView map, MapTheme mapTheme)
        {
            if (map is null || !OperatingSystem.IsIOSVersionAtLeast(13))
                return;

#pragma warning disable CA1416 // Validate platform compatibility
            map.OverrideUserInterfaceStyle = mapTheme switch
            {
                MapTheme.System => UIUserInterfaceStyle.Unspecified,
                MapTheme.Light => UIUserInterfaceStyle.Light,
                MapTheme.Dark => UIUserInterfaceStyle.Dark,
                _ => throw new NotSupportedException($"Unknown map theme '{mapTheme}'")
            };
#pragma warning restore CA1416 // Validate platform compatibility
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
#pragma warning disable CA1416 // Validate platform compatibility
                map.PointOfInterestFilter = new MKPointOfInterestFilter(Array.Empty<MKPointOfInterestCategory>());
#pragma warning restore CA1416 // Validate platform compatibility
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

        internal static void UpdateShowUserLocationButton(this MauiMapView map, bool showUserLocationButton)
        {
            if (map is null)
                return;
            if (map.UserTrackingButton is null)
                return;

            if (!showUserLocationButton && map.UserTrackingButton.Superview is not null)
            {
                map.UserTrackingButton.RemoveFromSuperview();
                return;
            }

            if (showUserLocationButton && map.UserTrackingButton.Superview is null)
            {
                const float utSize = 48f;
                map.UserTrackingButton.Layer.CornerRadius = utSize / 2;
                map.UserTrackingButton.Layer.BorderWidth = 0.25f;

                var circleMask = new CoreAnimation.CAShapeLayer();
                var circlePath = UIBezierPath.FromRoundedRect(new CGRect(0, 0, utSize, utSize), utSize / 2);
                circleMask.Path = circlePath.CGPath;
                map.UserTrackingButton.Layer.Mask = circleMask;

                map.UserTrackingButton.TranslatesAutoresizingMaskIntoConstraints = false;

                map.AddSubview(map.UserTrackingButton);

                var margins = map.LayoutMarginsGuide;
                NSLayoutConstraint.ActivateConstraints(new[]
                {
                    map.UserTrackingButton.BottomAnchor.ConstraintEqualTo(margins.BottomAnchor, -46),
                    map.UserTrackingButton.TrailingAnchor.ConstraintEqualTo(margins.TrailingAnchor, -12),
                    map.UserTrackingButton.WidthAnchor.ConstraintEqualTo(utSize),
                    map.UserTrackingButton.HeightAnchor.ConstraintEqualTo(map.UserTrackingButton.WidthAnchor),
                });
            }
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
