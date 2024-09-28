using CoreGraphics;
using MapKit;
using UIKit;

namespace BetterMaps.Maui.iOS
{
    internal static class MKMapViewViewExtensions
    {
        internal static void UpdateTheme(this MKMapView map, MapTheme mapTheme)
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

        internal static void UpdateType(this MKMapView map, MapType type)
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
                map.PointOfInterestFilter = new MKPointOfInterestFilter([]);
#pragma warning restore CA1416 // Validate platform compatibility
            }
            else
            {
                map.ShowsPointsOfInterest = false;
            }
        }

        internal static void UpdateIsShowingUser(this MKMapView map, bool isShowingUser)
        {
            if (map is null)
                return;

            if (isShowingUser)
                MauiMapView.LocationManager.RequestWhenInUseAuthorization();

            map.ShowsUserLocation = isShowingUser;
        }

        internal static void UpdateShowUserLocationButton(this MKMapView map, MKUserTrackingButton button, bool showUserLocationButton)
        {
            const float utSize = 48f;

            if (map is null)
                return;
            if (button is null)
                return;

            if (!showUserLocationButton && button.Superview is not null)
            {
                NSLayoutConstraint.DeactivateConstraints(getUserButtonConstraints(map, button));
                button.RemoveFromSuperview();
                return;
            }

            if (showUserLocationButton && button.Superview is null)
            {
                if (button.Layer.Mask is null)
                {
                    button.Layer.CornerRadius = utSize / 2;
                    button.Layer.BorderWidth = 0.25f;

                    var circleMask = new CoreAnimation.CAShapeLayer();
                    var circlePath = UIBezierPath.FromRoundedRect(new CGRect(0, 0, utSize, utSize), utSize / 2);
                    circleMask.Path = circlePath.CGPath;
                    button.Layer.Mask = circleMask;
                }

                map.AddSubview(button);
                button.TranslatesAutoresizingMaskIntoConstraints = false;

                NSLayoutConstraint.ActivateConstraints(getUserButtonConstraints(map, button));
            }

            static NSLayoutConstraint[] getUserButtonConstraints(MKMapView map, MKUserTrackingButton button)
            {
                var margins = map.LayoutMarginsGuide;
                return new[]
                {
                    button.BottomAnchor.ConstraintEqualTo(margins.BottomAnchor, -46),
                    button.TrailingAnchor.ConstraintEqualTo(margins.TrailingAnchor, -12),
                    button.WidthAnchor.ConstraintEqualTo(utSize),
                    button.HeightAnchor.ConstraintEqualTo(button.WidthAnchor),
                };
            }
        }

        internal static void UpdateShowCompass(this MKMapView map, bool showCompass)
        {
            if (map is null)
                return;
            map.ShowsCompass = showCompass;
        }

        internal static void UpdateHasScrollEnabled(this MKMapView map, bool hasScrollEnabled)
        {
            if (map is null)
                return;
            map.ScrollEnabled = hasScrollEnabled;
        }

        internal static void UpdateHasZoomEnabled(this MKMapView map, bool hasZoomEnabled)
        {
            if (map is null)
                return;
            map.ZoomEnabled = hasZoomEnabled;
        }

        internal static void UpdateTrafficEnabled(this MKMapView map, bool trafficEnabled)
        {
            if (map is null)
                return;
            map.ShowsTraffic = trafficEnabled;
        }
    }
}
