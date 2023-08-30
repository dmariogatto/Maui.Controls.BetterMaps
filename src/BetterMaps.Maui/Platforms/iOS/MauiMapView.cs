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
        private readonly WeakReference<MKUserTrackingButton> _userTrackingButtonRef;

        private bool _disposed;

        public MauiMapView(IMapHandler mapHandler) : base()
        {
            _handlerRef = new WeakReference<IMapHandler>(mapHandler);
            _userTrackingButtonRef = new WeakReference<MKUserTrackingButton>(null);
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
            => (_locationManagerRef ??= new (new ())).TryGetTarget(out var locManager) ? locManager : null;

        public MKUserTrackingButton UserTrackingButton
        {
            get
            {
                if (_disposed)
                    return null;

                if (!_userTrackingButtonRef.TryGetTarget(out var trackingBtn))
                {
                    trackingBtn = MKUserTrackingButton.FromMapView(this);
                    trackingBtn.UpdateTheme(IsDarkMode);
                    _userTrackingButtonRef.SetTarget(trackingBtn);
                }

                return trackingBtn;
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
                _userTrackingButtonRef.TryGetTarget(out var trackingBtn) &&
                TraitCollection?.UserInterfaceStyle != previousTraitCollection?.UserInterfaceStyle)
            {
                trackingBtn.UpdateTheme(IsDarkMode);
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
                if (_userTrackingButtonRef.TryGetTarget(out var trackingBtn))
                {
                    _userTrackingButtonRef.SetTarget(null);

                    trackingBtn.RemoveFromSuperview();
                    trackingBtn.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
