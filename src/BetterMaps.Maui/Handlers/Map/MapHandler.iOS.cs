using BetterMaps.Maui.iOS;
using CoreLocation;
using MapKit;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System.Collections.Specialized;
using System.ComponentModel;
using UIKit;

namespace BetterMaps.Maui.Handlers
{
    public partial class MapHandler : ViewHandler<IMap, MauiMapView>, IMapHandler
    {
        private readonly Dictionary<IMKAnnotation, Pin> _pinLookup = new Dictionary<IMKAnnotation, Pin>(ReferenceEqualityComparer.Instance);
        private readonly Dictionary<IMKOverlay, MapElement> _elementLookup = new Dictionary<IMKOverlay, MapElement>(ReferenceEqualityComparer.Instance);

        private bool _init = true;
        private bool _shouldUpdateRegion;

        private MKMapView _mapView;

        private UITapGestureRecognizer _mapClickedGestureRecognizer;
        private UILongPressGestureRecognizer _mapLongClickedGestureRecognizer;

        #region Overrides

        protected override MauiMapView CreatePlatformView()
            => new MauiMapView(this);

        protected override void ConnectHandler(MauiMapView platformView)
        {
            if (_mapView is not null)
                return;

            _mapView = platformView.CreateMap();

            platformView.OnLayoutSubviews += OnLayoutSubviews;

            _mapView.GetViewForAnnotation = MapPinHandler.GetViewForAnnotation;
            _mapView.OverlayRenderer = MapElementHandler.GetViewForOverlay;
            _mapView.DidSelectAnnotationView += MkMapViewOnAnnotationViewSelected;
            _mapView.DidDeselectAnnotationView += MkMapViewOnAnnotationViewDeselected;
            _mapView.RegionChanged += MkMapViewOnRegionChanged;

            platformView.AddGestureRecognizer(_mapClickedGestureRecognizer = new UITapGestureRecognizer(OnMapClicked));
            platformView.AddGestureRecognizer(_mapLongClickedGestureRecognizer = new UILongPressGestureRecognizer(OnMapLongClicked)
            {
                MinimumPressDuration = 1d,
                ShouldRecognizeSimultaneously = new UIGesturesProbe((recognizer, otherRecognizer) =>
                {
                    return otherRecognizer is UIPanGestureRecognizer;
                })
            });

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
        }

        protected override void DisconnectHandler(MauiMapView platformView)
        {
            if (_mapView is null)
                return;

            CleanUpMapModelElements();
            CleanUpNativeMap();

            _mapView = null;

            platformView.OnLayoutSubviews -= OnLayoutSubviews;
            platformView.DisposeMap();
        }

        public static void MapMapTheme(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._mapView?.UpdateTheme(map.MapTheme);
        }

        public static void MapMapType(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._mapView?.UpdateType(map.MapType);
        }

        public static void MapIsShowingUser(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._mapView?.UpdateIsShowingUser(map.IsShowingUser);
        }

        public static void MapShowUserLocationButton(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._mapView?.UpdateShowUserLocationButton(handler.PlatformView?.UserTrackingButton, map.ShowUserLocationButton);
        }

        public static void MapShowCompass(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._mapView?.UpdateShowCompass(map.ShowCompass);
        }

        public static void MapHasScrollEnabled(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._mapView?.UpdateHasScrollEnabled(map.HasScrollEnabled);
        }

        public static void MapHasZoomEnabled(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._mapView?.UpdateHasZoomEnabled(map.HasZoomEnabled);
        }

        public static void MapTrafficEnabled(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._mapView?.UpdateTrafficEnabled(map.TrafficEnabled);
        }

        public static void MapSelectedPin(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?.UpdateSelectedPin();
        }

        public static void MapHeight(IMapHandler handler, IMap map)
        {
            if (handler is not MapHandler mapHandler)
                return;

            if (mapHandler.VirtualView?.LastMoveToRegion is not null)
                mapHandler._shouldUpdateRegion = mapHandler.VirtualView.MoveToLastRegionOnLayoutChange;
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
                        _mapView.SetCenterCoordinate(_mapView.CenterCoordinate, false);
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

            if (!PinTapped(recognizer))
            {
                var tapPoint = recognizer.LocationInView(PlatformView);
                var tapGPS = _mapView.ConvertPoint(tapPoint, PlatformView);
                VirtualView.SendMapClicked(new Position(tapGPS.Latitude, tapGPS.Longitude));
            }
        }

        private void OnMapLongClicked(UILongPressGestureRecognizer recognizer)
        {
            if (recognizer.State != UIGestureRecognizerState.Began)
                return;
            if (VirtualView?.CanSendMapLongClicked() != true)
                return;

            if (!PinTapped(recognizer))
            {
                var tapPoint = recognizer.LocationInView(PlatformView);
                var tapGPS = _mapView.ConvertPoint(tapPoint, PlatformView);
                VirtualView.SendMapLongClicked(new Position(tapGPS.Latitude, tapGPS.Longitude));
            }
        }

        private bool PinTapped(UIGestureRecognizer recognizer)
        {
            var pinTapped = _mapView.Annotations
                .Select(a => _mapView.ViewForAnnotation(a))
                .Where(v => v is not null)
                .Any(v => v.PointInside(recognizer.LocationInView(v), null));
            return pinTapped;
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

            var pos = new Position(_mapView.Region.Center.Latitude, _mapView.Region.Center.Longitude);
            VirtualView.SetVisibleRegion(new MapSpan(pos, _mapView.Region.Span.LatitudeDelta, _mapView.Region.Span.LongitudeDelta, _mapView.Camera.Heading));
        }

        private void MoveToRegion(MapSpan mapSpan, bool animated = true)
            => _mapView.SetRegion(MapSpanToMKCoordinateRegion(mapSpan), animated);

        private MKCoordinateRegion MapSpanToMKCoordinateRegion(MapSpan mapSpan)
            => new MKCoordinateRegion(new CLLocationCoordinate2D(mapSpan.Center.Latitude, mapSpan.Center.Longitude), new MKCoordinateSpan(mapSpan.LatitudeDegrees, mapSpan.LongitudeDegrees));

        private void UpdateSelectedPin()
        {
            var pin = VirtualView.SelectedPin;

            if (pin is null)
            {
                foreach (var a in _mapView.SelectedAnnotations)
                    _mapView.DeselectAnnotation(a, false);
            }
            else if (pin.NativeId is IMKAnnotation annotation)
            {
                _mapView.SelectAnnotation(annotation, false);
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
            var annotations = pins
                .Select(p => (IMKAnnotation)p.NativeId)
                .ToArray();

            var selectedToRemove = annotations.Intersect(_mapView.SelectedAnnotations ?? []).ToList();

            foreach (var a in selectedToRemove)
                _mapView.DeselectAnnotation(a, false);

            _mapView.RemoveAnnotations(annotations);

            foreach (var p in pins)
            {
                p.Handler?.DisconnectHandler();

                _pinLookup.Remove((IMKAnnotation)p.NativeId);
                p.NativeId = null;

                p.ImageSourceCts?.Cancel();
                p.ImageSourceCts?.Dispose();
                p.ImageSourceCts = null;
            }
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

            _mapView.AddAnnotations(annotations);

            if (selectedAnnotation is not null)
                _mapView.SelectAnnotation(selectedAnnotation, true);
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

            _mapView.AddOverlays(overlays);
        }

        private void RemoveMapElements(IEnumerable<MapElement> mapElements)
        {
            var overlays = mapElements
                .Select(e => (IMKOverlay)e.MapElementId)
                .ToArray();

            _mapView.RemoveOverlays(overlays);

            foreach (var e in mapElements)
            {
                if (e.Handler is MapElementHandler mapElementHandler)
                    mapElementHandler.OnRecreateRequested -= MapElementHandlerOnRecreateRequested;

                e.Handler?.DisconnectHandler();

                _elementLookup.Remove((IMKOverlay)e.MapElementId);
                e.MapElementId = null;
            }
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

        private void CleanUpMapModelElements()
        {
            VirtualView.PropertyChanged -= OnVirtualViewPropertyChanged;
            VirtualView.Pins.CollectionChanged -= OnPinCollectionChanged;
            VirtualView.MapElements.CollectionChanged -= OnMapElementCollectionChanged;

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

            if (_mapView?.SelectedAnnotations?.Length > 0)
                foreach (var sa in _mapView.SelectedAnnotations.ToList())
                    _mapView.DeselectAnnotation(sa, false);


            _mapView?.RemoveAnnotations(_pinLookup.Keys.ToArray());
            _mapView?.RemoveOverlays(_elementLookup.Keys.ToArray());

            _pinLookup.Clear();
            _elementLookup.Clear();
        }

        private void CleanUpNativeMap()
        {
            _mapView.GetViewForAnnotation = null;
            _mapView.OverlayRenderer = null;
            _mapView.DidSelectAnnotationView -= MkMapViewOnAnnotationViewSelected;
            _mapView.DidDeselectAnnotationView -= MkMapViewOnAnnotationViewDeselected;
            _mapView.RegionChanged -= MkMapViewOnRegionChanged;

            _mapView.Delegate?.Dispose();
            _mapView.Delegate = null;

            if (_mapClickedGestureRecognizer is not null)
            {
                _mapView.RemoveGestureRecognizer(_mapClickedGestureRecognizer);
                _mapClickedGestureRecognizer.Dispose();
                _mapClickedGestureRecognizer = null;
            }

            if (_mapLongClickedGestureRecognizer is not null)
            {
                _mapView.RemoveGestureRecognizer(_mapLongClickedGestureRecognizer);
                _mapLongClickedGestureRecognizer.Dispose();
                _mapLongClickedGestureRecognizer = null;
            }

            if (_mapView.Annotations?.Length > 0)
                _mapView.RemoveAnnotations(_mapView.Annotations.ToArray());

            if (_mapView.Overlays?.Length > 0)
                _mapView.RemoveOverlays(_mapView.Overlays.ToArray());
        }
    }
}
