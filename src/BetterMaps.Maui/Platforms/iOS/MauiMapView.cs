using BetterMaps.Maui.Handlers;
using CoreGraphics;
using CoreLocation;
using Foundation;
using MapKit;
using ObjCRuntime;
using UIKit;

namespace BetterMaps.Maui.iOS
{
    public class MauiMapView : MKMapView
    {
        private readonly WeakEventManager _wem = new WeakEventManager();

        private readonly WeakReference<IMapHandler> _handlerRef;

        private MKUserTrackingButton _userTrackingButton;

        private bool _disposed;

        public MauiMapView(IMapHandler mapHandler) : base()
        {
            _handlerRef = new WeakReference<IMapHandler>(mapHandler);
        }

        public MauiMapView(CGRect frame) : base(frame) { }
        public MauiMapView(NSCoder coder) : base(coder) { }
        public MauiMapView(NSObjectFlag t) : base(t) { }
        public MauiMapView(NativeHandle handle) : base(handle) { }

        public event EventHandler<EventArgs> OnLayoutSubviews
        {
            add => _wem.AddEventHandler(value);
            remove => _wem.RemoveEventHandler(value);
        }

        public event EventHandler<UITraitCollection> OnTraitCollectionDidChange
        {
            add => _wem.AddEventHandler(value);
            remove => _wem.RemoveEventHandler(value);
        }

        public bool IsDarkMode =>
            OperatingSystem.IsIOSVersionAtLeast(13) &&
            TraitCollection?.UserInterfaceStyle == UIUserInterfaceStyle.Dark;

        private WeakReference<CLLocationManager> _locationManagerRef;
        public CLLocationManager LocationManager
        {
            get
            {
                _locationManagerRef ??= new WeakReference<CLLocationManager>(new CLLocationManager());

                if (!_locationManagerRef.TryGetTarget(out var locationManager))
                {
                    locationManager = new CLLocationManager();
                    _locationManagerRef.SetTarget(locationManager);
                }

                return locationManager;
            }
        }

        public MKUserTrackingButton UserTrackingButton
        {
            get
            {
                if (_disposed)
                    return null;

                if (_userTrackingButton is null)
                {
                    _userTrackingButton = MKUserTrackingButton.FromMapView(this);
                    _userTrackingButton.UpdateTheme(IsDarkMode);
                }

                return _userTrackingButton;
            }
        }

        public IElement VirtualViewForAnnotation(IMKAnnotation annotation)
            => _handlerRef?.TryGetTarget(out var handler) == true &&
               handler is MapHandler mapHandler
               ? mapHandler.GetPinForAnnotation(annotation)
               : null;

        public IElement VirtualViewForOverlay(IMKOverlay overlay)
            => _handlerRef?.TryGetTarget(out var handler) == true &&
               handler is MapHandler mapHandler
               ? mapHandler.GetMapElementForOverlay(overlay)
               : null;

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            _wem.HandleEvent(this, new EventArgs(), nameof(OnLayoutSubviews));
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            if (OperatingSystem.IsIOSVersionAtLeast(13) &&
                _userTrackingButton is not null &&
                TraitCollection?.UserInterfaceStyle != previousTraitCollection?.UserInterfaceStyle)
            {
                _userTrackingButton.UpdateTheme(IsDarkMode);
            }

            _wem.HandleEvent(this, previousTraitCollection, nameof(OnTraitCollectionDidChange));
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            _disposed = true;

            if (disposing)
            {
                if (_userTrackingButton is not null)
                {
                    _userTrackingButton.RemoveFromSuperview();
                    _userTrackingButton.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
