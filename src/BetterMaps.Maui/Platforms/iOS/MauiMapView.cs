using BetterMaps.Maui.Handlers;
using CoreGraphics;
using CoreLocation;
using Foundation;
using MapKit;
using ObjCRuntime;
using UIKit;

namespace BetterMaps.Maui.iOS
{
    public class MauiMapView : UIView
    {
        private static readonly WeakEventManager WeakEventManager = new WeakEventManager();

        private static readonly Lazy<CLLocationManager> LazyLocationManager = new Lazy<CLLocationManager>(() => new CLLocationManager());
        public static CLLocationManager LocationManager => LazyLocationManager.Value;

        private MapHandler _handler;

        private MKMapView _mapView;
        private MKUserTrackingButton _userTrackingButton;

        private bool _disposed;

        public MauiMapView(MapHandler mapHandler) : base()
        {
            _handler = mapHandler;
        }

        public MauiMapView(CGRect frame) : base(frame) { }
        public MauiMapView(NSCoder coder) : base(coder) { }
        public MauiMapView(NSObjectFlag t) : base(t) { }
        public MauiMapView(NativeHandle handle) : base(handle) { }

        public event EventHandler<EventArgs> OnLayoutSubviews
        {
            add => WeakEventManager.AddEventHandler(value);
            remove => WeakEventManager.RemoveEventHandler(value);
        }

        public event EventHandler<UITraitCollection> OnTraitCollectionDidChange
        {
            add => WeakEventManager.AddEventHandler(value);
            remove => WeakEventManager.RemoveEventHandler(value);
        }

        public bool IsDarkMode =>
            OperatingSystem.IsIOSVersionAtLeast(13) &&
            TraitCollection?.UserInterfaceStyle == UIUserInterfaceStyle.Dark;

        public MKMapView Map
        {
            get => _mapView;
        }

        public MKUserTrackingButton UserTrackingButton
        {
            get
            {
                if (_disposed)
                    return null;

                if (_mapView is not null && _userTrackingButton is null)
                {
                    _userTrackingButton = MKUserTrackingButton.FromMapView(_mapView);
                    _userTrackingButton.UpdateTheme(IsDarkMode);
                }

                return _userTrackingButton;
            }
        }

        public MKMapView CreateMap()
        {
            if (_disposed)
                return null;

            if (_mapView is null)
            {
                _mapView = new MKMapView();
                _mapView.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
                AddSubview(_mapView);
            }

            return _mapView;
        }

        public void DisposeMap()
        {
            if (_userTrackingButton is not null)
            {
                _userTrackingButton.RemoveFromSuperview();
                _userTrackingButton.Dispose();
                _userTrackingButton = null;
            }

            if (_mapView is not null)
            {
                _mapView.RemoveFromSuperview();
                _mapView.Dispose();
                _mapView = null;
            }
        }

        public IElement VirtualViewForAnnotation(IMKAnnotation annotation)
            => _handler?.GetPinForAnnotation(annotation);

        public IElement VirtualViewForOverlay(IMKOverlay overlay)
            => _handler?.GetMapElementForOverlay(overlay);

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            WeakEventManager.HandleEvent(this, new EventArgs(), nameof(OnLayoutSubviews));
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
#pragma warning disable CA1422 // Validate platform compatibility
            base.TraitCollectionDidChange(previousTraitCollection);
#pragma warning restore CA1422 // Validate platform compatibility

            if (OperatingSystem.IsIOSVersionAtLeast(13) &&
                _userTrackingButton is not null &&
                TraitCollection?.UserInterfaceStyle != previousTraitCollection?.UserInterfaceStyle)
            {
                _userTrackingButton.UpdateTheme(IsDarkMode);
            }

            WeakEventManager.HandleEvent(this, previousTraitCollection, nameof(OnTraitCollectionDidChange));
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                _handler = null;
                DisposeMap();
            }

            base.Dispose(disposing);
        }
    }
}
