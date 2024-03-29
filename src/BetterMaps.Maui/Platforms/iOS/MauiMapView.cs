﻿using BetterMaps.Maui.Handlers;
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
        private readonly WeakReference<IMapHandler> _handlerRef;

        private bool _disposed;
        private MKUserTrackingButton _userTrackingButton;

        public MauiMapView(IMapHandler mapHandler) : base()
        {
            _handlerRef = new WeakReference<IMapHandler>(mapHandler);
        }

        public MauiMapView(CGRect frame) : base(frame) { }
        public MauiMapView(NSCoder coder) : base(coder) { }
        public MauiMapView(NSObjectFlag t) : base(t) { }
        public MauiMapView(NativeHandle handle) : base(handle) { }

        public event EventHandler<EventArgs> OnLayoutSubviews;
        public event EventHandler<UITraitCollection> OnTraitCollectionDidChange;

        public bool IsDarkMode =>
            OperatingSystem.IsIOSVersionAtLeast(13) &&
            TraitCollection?.UserInterfaceStyle == UIUserInterfaceStyle.Dark;

        private CLLocationManager _locationManager;
        public CLLocationManager LocationManager => _locationManager ??= new CLLocationManager();

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

            OnLayoutSubviews?.Invoke(this, new EventArgs());
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
