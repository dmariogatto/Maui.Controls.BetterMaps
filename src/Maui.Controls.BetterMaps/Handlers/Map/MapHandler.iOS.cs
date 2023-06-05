using CoreLocation;
using MapKit;
using Maui.Controls.BetterMaps.iOS;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System.Collections.Specialized;
using System.ComponentModel;
using UIKit;

namespace Maui.Controls.BetterMaps.Handlers
{
    public partial class MapHandler : ViewHandler<IMap, MauiMapView>, IMapHandler
    {
        private readonly Dictionary<IMKAnnotation, Pin> _pinLookup = new Dictionary<IMKAnnotation, Pin>(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<IMKOverlay, MapElement> _elementLookup = new Dictionary<IMKOverlay, MapElement>(ReferenceEqualityComparer.Instance);

        private bool _shouldUpdateRegion;
        private bool _init = true;

        private UITapGestureRecognizer _mapClickedGestureRecognizer;

        #region Overrides

        protected override MauiMapView CreatePlatformView()
            => new MauiMapView(this);

        protected override void ConnectHandler(MauiMapView platformView)
        {
            platformView.GetViewForAnnotation = MapPinHandler.GetViewForAnnotation;
            platformView.OverlayRenderer = MapElementHandler.GetViewForOverlay;
            platformView.OnLayoutSubviews += OnLayoutSubviews;
            platformView.DidSelectAnnotationView += MkMapViewOnAnnotationViewSelected;
            platformView.DidDeselectAnnotationView += MkMapViewOnAnnotationViewDeselected;
            platformView.RegionChanged += MkMapViewOnRegionChanged;
            platformView.AddGestureRecognizer(_mapClickedGestureRecognizer = new UITapGestureRecognizer(OnMapClicked));

            MapMapTheme(this, VirtualView);
            MapMapType(this, VirtualView);
            MapIsShowingUser(this, VirtualView);
            MapShowUserLocationButton(this, VirtualView);
            MapShowCompass(this, VirtualView);
            MapHasScrollEnabled(this, VirtualView);
            MapHasZoomEnabled(this, VirtualView);
            MapTrafficEnabled(this, VirtualView);

            VirtualView.PropertyChanged += OnVirtualViewPropertyChanged;

            VirtualView.Pins.CollectionChanged += OnPinCollectionChanged;
            OnPinCollectionChanged(VirtualView.Pins, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            VirtualView.MapElements.CollectionChanged += OnMapElementCollectionChanged;
            OnMapElementCollectionChanged(VirtualView.MapElements, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            UpdateSelectedPin();

            base.ConnectHandler(platformView);
        }

        protected override void DisconnectHandler(MauiMapView platformView)
        {
            if (VirtualView is not null)
            {
                CleanUpMapModelElements(VirtualView, PlatformView);
            }

            if (PlatformView is not null)
            {
                CleanUpNativeMap(PlatformView);
            }

            base.DisconnectHandler(platformView);
        }

        public static void MapMapTheme(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateTheme(map.MapTheme);
        }

        public static void MapMapType(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateType(map.MapType);
        }

        public static void MapIsShowingUser(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateIsShowingUser(map.IsShowingUser);
        }

        public static void MapShowUserLocationButton(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateShowUserLocationButton(map.ShowUserLocationButton);
        }

        public static void MapShowCompass(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateShowCompass(map.ShowCompass);
        }

        public static void MapHasScrollEnabled(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateHasScrollEnabled(map.HasScrollEnabled);
        }

        public static void MapHasZoomEnabled(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateHasZoomEnabled(map.HasZoomEnabled);
        }

        public static void MapTrafficEnabled(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateTrafficEnabled(map.TrafficEnabled);
        }

        public static void MapMoveToRegion(IMapHandler handler, IMap map, object arg)
        {
            if (arg is MapSpan mapSpan)
                (handler as MapHandler)?.MoveToRegion(mapSpan, true);
        }

        private void OnLayoutSubviews(object sender, EventArgs e)
        {
            // need to have frame define for this to work
            if (_init && PlatformView.Frame.Height > 1)
            {
                // initial region
                _init = false;
                if (VirtualView.LastMoveToRegion is not null)
                    MoveToRegion(VirtualView.LastMoveToRegion, false);
            }

            UpdateRegion();
        }

        private void OnVirtualViewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == VisualElement.HeightProperty.PropertyName && VirtualView.LastMoveToRegion is not null)
                _shouldUpdateRegion = VirtualView.MoveToLastRegionOnLayoutChange;
        }
        #endregion

        #region Annotations
        private void MkMapViewOnAnnotationViewSelected(object sender, MKAnnotationViewEventArgs e)
        {
            var annotation = e.View.Annotation;
            var pin = GetPinForAnnotation(annotation);

            if (pin is null)
                return;

            if (e.View.GestureRecognizers?.Length > 0)
                foreach (var r in e.View.GestureRecognizers.ToList())
                {
                    e.View.RemoveGestureRecognizer(r);
                    r.Dispose();
                }

            if (e.View.CanShowCallout)
            {
                var calloutTapRecognizer = new UITapGestureRecognizer(g => OnCalloutClicked(annotation));
                var calloutLongRecognizer = new UILongPressGestureRecognizer(g =>
                {
                    if (g.State == UIGestureRecognizerState.Began)
                    {
                        OnCalloutAltClicked(annotation);
                        // workaround (long press not registered until map movement)
                        // https://developer.apple.com/forums/thread/126473
                        PlatformView.SetCenterCoordinate(PlatformView.CenterCoordinate, false);
                    }
                });

                e.View.AddGestureRecognizer(calloutTapRecognizer);
                e.View.AddGestureRecognizer(calloutLongRecognizer);
            }
            else
            {
                var pinTapRecognizer = new UITapGestureRecognizer(g => OnPinClicked(annotation));
                e.View.AddGestureRecognizer(pinTapRecognizer);
            }

            if (!ReferenceEquals(pin, VirtualView.SelectedPin))
            {
                VirtualView.SelectedPin = pin;
            }

            VirtualView.SendPinClick(pin);
        }

        private void MkMapViewOnAnnotationViewDeselected(object sender, MKAnnotationViewEventArgs e)
        {
            if (e.View.GestureRecognizers?.Length > 0)
                foreach (var r in e.View.GestureRecognizers.ToList())
                {
                    e.View.RemoveGestureRecognizer(r);
                    r.Dispose();
                }

            if (GetPinForAnnotation(e.View.Annotation) is Pin pin &&
                ReferenceEquals(VirtualView.SelectedPin, pin))
            {
                VirtualView.SelectedPin = null;
            }
        }

        private void OnPinClicked(IMKAnnotation annotation)
        {
            if (GetPinForAnnotation(annotation) is Pin pin)
                VirtualView.SendPinClick(pin);
        }

        private void OnCalloutClicked(IMKAnnotation annotation)
        {
            if (GetPinForAnnotation(annotation) is Pin pin)
                VirtualView.SendInfoWindowClick(pin);
        }

        private void OnCalloutAltClicked(IMKAnnotation annotation)
        {
            if (GetPinForAnnotation(annotation) is Pin pin)
                VirtualView.SendInfoWindowLongClick(pin);
        }
        #endregion

        #region Map
        private void OnMapClicked(UITapGestureRecognizer recognizer)
        {
            if (VirtualView?.CanSendMapClicked() != true)
                return;

            var pinTapped = PlatformView.Annotations
                .Select(a => PlatformView.ViewForAnnotation(a))
                .Where(v => v is not null)
                .Any(v => v.PointInside(recognizer.LocationInView(v), null));

            if (!pinTapped)
            {
                var tapPoint = recognizer.LocationInView(PlatformView);
                var tapGPS = PlatformView.ConvertPoint(tapPoint, PlatformView);
                VirtualView.SendMapClicked(new Position(tapGPS.Latitude, tapGPS.Longitude));
            }
        }

        private void UpdateRegion()
        {
            if (_shouldUpdateRegion)
            {
                MoveToRegion(VirtualView.LastMoveToRegion, false);
                _shouldUpdateRegion = false;
            }
        }

        private void MkMapViewOnRegionChanged(object sender, MKMapViewChangeEventArgs e)
        {
            if (VirtualView is null)
                return;

            var pos = new Position(PlatformView.Region.Center.Latitude, PlatformView.Region.Center.Longitude);
            VirtualView.SetVisibleRegion(new MapSpan(pos, PlatformView.Region.Span.LatitudeDelta, PlatformView.Region.Span.LongitudeDelta, PlatformView.Camera.Heading));
        }

        private void MoveToRegion(MapSpan mapSpan, bool animated = true)
            => PlatformView.SetRegion(MapSpanToMKCoordinateRegion(mapSpan), animated);

        private MKCoordinateRegion MapSpanToMKCoordinateRegion(MapSpan mapSpan)
            => new MKCoordinateRegion(new CLLocationCoordinate2D(mapSpan.Center.Latitude, mapSpan.Center.Longitude), new MKCoordinateSpan(mapSpan.LatitudeDegrees, mapSpan.LongitudeDegrees));

        private void UpdateSelectedPin()
        {
            var pin = VirtualView.SelectedPin;

            if (pin is null)
            {
                foreach (var a in PlatformView.SelectedAnnotations)
                    PlatformView.DeselectAnnotation(a, false);
            }
            else if (pin.NativeId is IMKAnnotation annotation)
            {
                PlatformView.SelectAnnotation(annotation, false);
            }
        }


        #endregion

        #region Pins
        private void OnPinCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (VirtualView is BindableObject bo && bo.Dispatcher.IsDispatchRequired)
                bo.Dispatcher.Dispatch(() => PinCollectionChanged(e));
            else
                PinCollectionChanged(e);
        }

        private void PinCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var itemsToAdd = e.NewItems?.Cast<Pin>()?.ToList() ?? new List<Pin>(0);
            var itemsToRemove = e.OldItems?.Cast<Pin>()?.Where(p => p.NativeId is not null)?.ToList() ?? new List<Pin>(0);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddPins(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemovePins(itemsToRemove);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemovePins(itemsToRemove);
                    AddPins(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemovePins(_pinLookup.Values.ToList());

                    AddPins(VirtualView.Pins);
                    break;
                case NotifyCollectionChangedAction.Move:
                    //do nothing
                    break;
            }
        }

        private void RemovePins(IList<Pin> pins)
        {
            var annotations = pins.Select(p =>
            {
                p.Handler?.DisconnectHandler();

                var annotation = (IMKAnnotation)p.NativeId;
                _pinLookup.Remove(annotation);
                p.NativeId = null;

                p.ImageSourceCts?.Cancel();
                p.ImageSourceCts?.Dispose();
                p.ImageSourceCts = null;

                return annotation;
            }).ToArray();

            var selectedToRemove =
                (from sa in PlatformView.SelectedAnnotations ?? Array.Empty<IMKAnnotation>()
                 join a in annotations on sa equals a
                 select sa).ToList();

            foreach (var a in selectedToRemove)
                PlatformView.DeselectAnnotation(a, false);

            PlatformView.RemoveAnnotations(annotations);
        }

        private void AddPins(IList<Pin> pins)
        {
            if (!pins.Any())
                return;

            var selectedAnnotation = default(IMKAnnotation);

            var annotations = pins
                .Select(p => p.ToHandler(MauiContext))
                .OfType<IMapPinHandler>()
                .Select(h =>
                {
                    var pin = h.VirtualView;
                    var annotation = h.PlatformView;

                    pin.NativeId = annotation;

                    if (ReferenceEquals(pin, VirtualView.SelectedPin))
                        selectedAnnotation = annotation;

                    _pinLookup.Add(annotation, (Pin)pin);

                    return annotation;
                }).ToArray();

            PlatformView.AddAnnotations(annotations);

            if (selectedAnnotation is not null)
                PlatformView.SelectAnnotation(selectedAnnotation, true);
        }

        internal Pin GetPinForAnnotation(IMKAnnotation annotation)
            => annotation is not null && _pinLookup.TryGetValue(annotation, out var p) ? p : null;
        #endregion

        #region MapElements
        private void OnMapElementCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (VirtualView is BindableObject bo && bo.Dispatcher.IsDispatchRequired)
                bo.Dispatcher.Dispatch(() => MapElementCollectionChanged(e));
            else
                MapElementCollectionChanged(e);
        }

        private void MapElementCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            var itemsToAdd = e.NewItems?.Cast<MapElement>()?.ToList() ?? new List<MapElement>(0);
            var itemsToRemove = e.OldItems?.Cast<MapElement>()?.ToList() ?? new List<MapElement>(0);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddMapElements(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveMapElements(itemsToRemove);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveMapElements(itemsToRemove);
                    AddMapElements(itemsToAdd);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemoveMapElements(_elementLookup.Values.ToList());

                    AddMapElements(VirtualView.MapElements);
                    break;
            }
        }

        private void AddMapElements(IEnumerable<MapElement> mapElements)
        {
            if (!mapElements.Any())
                return;

            var overlays = mapElements.Select(p => p.ToHandler(MauiContext))
                .OfType<IMapElementHandler>()
                .Select(h =>
                {
                    var mapElement = h.VirtualView;
                    var overlay = h.PlatformView;

                    if (h is MapElementHandler mapElementHandler)
                        mapElementHandler.OnRecreateRequested += MapElementHandlerOnRecreateRequested;

                    mapElement.MapElementId = overlay;

                    _elementLookup.Add(overlay, (MapElement)mapElement);

                    return overlay;
                }).ToArray();

            PlatformView.AddOverlays(overlays);
        }

        private void RemoveMapElements(IEnumerable<MapElement> mapElements)
        {
            var overlays = mapElements.Select(e =>
            {
                if (e.Handler is MapElementHandler mapElementHandler)
                    mapElementHandler.OnRecreateRequested -= MapElementHandlerOnRecreateRequested;

                e.Handler?.DisconnectHandler();

                var overlay = (IMKOverlay)e.MapElementId;
                _elementLookup.Remove(overlay);
                e.MapElementId = null;

                return overlay;
            }).ToArray();

            PlatformView.RemoveOverlays(overlays);
        }

        private void MapElementHandlerOnRecreateRequested(object sender, EventArgs e)
        {
            var mapElementHandler = (MapElementHandler)sender;

            RemoveMapElements(new[] { (MapElement)mapElementHandler.VirtualView });
            AddMapElements(new[] { (MapElement)mapElementHandler.VirtualView });
        }

        internal MapElement GetMapElementForOverlay(IMKOverlay overlay)
            => overlay is not null && _elementLookup.TryGetValue(overlay, out var e) ? e : null;
        #endregion

        private void CleanUpMapModelElements(IMap mapModel, MKMapView mapNative)
        {
            mapModel.PropertyChanged -= OnVirtualViewPropertyChanged;
            mapModel.Pins.CollectionChanged -= OnPinCollectionChanged;
            mapModel.MapElements.CollectionChanged -= OnMapElementCollectionChanged;

            foreach (var kv in _pinLookup)
            {
                kv.Value.NativeId = null;
            }

            foreach (var kv in _elementLookup)
            {
                if (kv.Value.Handler is MapElementHandler mapElementHandler)
                    mapElementHandler.OnRecreateRequested -= MapElementHandlerOnRecreateRequested;
                kv.Value.MapElementId = null;
            }

            if (mapNative?.SelectedAnnotations?.Length > 0)
                foreach (var sa in mapNative.SelectedAnnotations.ToList())
                    mapNative.DeselectAnnotation(sa, false);


            mapNative?.RemoveAnnotations(_pinLookup.Keys.ToArray());
            mapNative?.RemoveOverlays(_elementLookup.Keys.ToArray());

            _pinLookup.Clear();
            _elementLookup.Clear();
        }

        private void CleanUpNativeMap(MauiMapView mapNative)
        {
            mapNative.GetViewForAnnotation = null;
            mapNative.OverlayRenderer = null;
            mapNative.DidSelectAnnotationView -= MkMapViewOnAnnotationViewSelected;
            mapNative.DidDeselectAnnotationView -= MkMapViewOnAnnotationViewDeselected;
            mapNative.RegionChanged -= MkMapViewOnRegionChanged;

            mapNative.OnLayoutSubviews -= OnLayoutSubviews;

            mapNative.Delegate?.Dispose();
            mapNative.Delegate = null;

            if (_mapClickedGestureRecognizer is not null)
            {
                mapNative.RemoveGestureRecognizer(_mapClickedGestureRecognizer);
                _mapClickedGestureRecognizer.Dispose();
                _mapClickedGestureRecognizer = null;
            }

            if (mapNative.Annotations?.Length > 0)
                mapNative.RemoveAnnotations(mapNative.Annotations.ToArray());

            if (mapNative.Overlays?.Length > 0)
                mapNative.RemoveOverlays(mapNative.Overlays.ToArray());
        }
    }
}
