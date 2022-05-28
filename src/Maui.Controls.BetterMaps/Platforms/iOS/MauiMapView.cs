using CoreGraphics;
using CoreLocation;
using Foundation;
using MapKit;
using ObjCRuntime;
using UIKit;

namespace Maui.Controls.BetterMaps.iOS
{
    public class MauiMapView : MKMapView
    {
        private bool _disposed;
        private MKUserTrackingButton _userTrackingButton;

        public MauiMapView() : base() { }
        public MauiMapView(CGRect frame) : base(frame) { }
        public MauiMapView(NSCoder coder) : base(coder) { }
        public MauiMapView(NSObjectFlag t) : base(t) { }
        public MauiMapView(NativeHandle handle) : base(handle) { }

        public event EventHandler<EventArgs> OnLayoutSubviews;
        public event EventHandler<UITraitCollection> OnTraitCollectionDidChange;

        public bool IsDarkMode =>
            MauiBetterMaps.Ios13OrNewer &&
            TraitCollection?.UserInterfaceStyle == UIUserInterfaceStyle.Dark;

        public CLLocationManager LocationManager
            => MauiBetterMaps.LocationManager;

        public MKUserTrackingButton UserTrackingButton
        {
            get
            {
                if (!_disposed && _userTrackingButton is null)
                {
                    _userTrackingButton = MKUserTrackingButton.FromMapView(this);
                    _userTrackingButton.UpdateTheme(IsDarkMode);
                }
                
                return _userTrackingButton;
            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            OnLayoutSubviews?.Invoke(this, new EventArgs());
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            if (MauiBetterMaps.Ios13OrNewer &&
                _userTrackingButton is not null &&
                TraitCollection?.UserInterfaceStyle != previousTraitCollection?.UserInterfaceStyle)
            {
                _userTrackingButton.UpdateTheme(IsDarkMode);
            }

            OnTraitCollectionDidChange?.Invoke(this, previousTraitCollection);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                _userTrackingButton?.RemoveFromSuperview();
                _userTrackingButton?.Dispose();
                _userTrackingButton = null;
            }

            base.Dispose(disposing);
        }
    }
}
