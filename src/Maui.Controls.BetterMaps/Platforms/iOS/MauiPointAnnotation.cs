using CoreGraphics;
using CoreLocation;
using MapKit;
using Microsoft.Maui.Platform;
using UIKit;

namespace Maui.Controls.BetterMaps.iOS
{
    internal class MauiPointAnnotation : MKPointAnnotation
    {
        public readonly Pin Pin;

        public MauiPointAnnotation(Pin pin) : base()
        {
            Pin = pin;

            Title = pin.Label;
            Subtitle = pin.Address ?? string.Empty;
            Coordinate = new CLLocationCoordinate2D(pin.Position.Latitude, pin.Position.Longitude);
        }

        public UIColor TintColor => Pin.TintColor.ToPlatform(Colors.Transparent);
        public CGPoint Anchor => new CGPoint(Pin.Anchor.X, Pin.Anchor.Y);
        public int ZIndex => Pin.ZIndex;
        public ImageSource ImageSource => Pin.ImageSource;
    }
}