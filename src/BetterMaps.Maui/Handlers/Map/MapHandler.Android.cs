using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Widget;
using BetterMaps.Maui.Android;
using Java.Lang;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System.Collections.Specialized;
using System.ComponentModel;
using ACircle = Android.Gms.Maps.Model.Circle;
using APolygon = Android.Gms.Maps.Model.Polygon;
using APolyline = Android.Gms.Maps.Model.Polyline;
using LP = Android.Views.ViewGroup.LayoutParams;
using Math = System.Math;

namespace BetterMaps.Maui.Handlers
{
    public partial class MapHandler : ViewHandler<IMap, FrameLayout>, IMapHandler
    {
        private readonly Dictionary<string, (Pin pin, Marker marker)> _markers = new Dictionary<string, (Pin, Marker)>(StringComparer.Ordinal);
        private readonly Dictionary<string, (Polyline element, APolyline polyline)> _polylines = new Dictionary<string, (Polyline, APolyline)>(StringComparer.Ordinal);
        private readonly Dictionary<string, (Polygon element, APolygon polygon)> _polygons = new Dictionary<string, (Polygon, APolygon)>(StringComparer.Ordinal);
        private readonly Dictionary<string, (Circle element, ACircle circle)> _circles = new Dictionary<string, (Circle, ACircle)>(StringComparer.Ordinal);

        private FrameLayout _rootLayout;
        private SupportMapFragment _fragment;
        private OnGoogleMapReadyCallback _mapReadyCallback;
        private GoogleMap _map;

        #region Overrides

        protected override FrameLayout CreatePlatformView()
        {
            _rootLayout ??= new FrameLayout(Context)
            {
                Id = FrameLayout.GenerateViewId(),
                LayoutParameters = new LP(LP.MatchParent, LP.MatchParent),
            };

            return _rootLayout;
        }

        protected override void ConnectHandler(FrameLayout platformView)
        {
            if (_fragment is not null)
                return;

            var fragmentManager = _rootLayout.Context.GetFragmentManager();
            if (fragmentManager is null || fragmentManager.IsDestroyed)
                return;

            VirtualView.PropertyChanged += OnVirtualViewPropertyChanged;
            VirtualView.Pins.CollectionChanged += OnPinCollectionChanged;
            VirtualView.MapElements.CollectionChanged += OnMapElementCollectionChanged;

            _rootLayout.ViewAttachedToWindow += OnViewAttachedToWindow;
            _rootLayout.LayoutChange += OnLayoutChange;

            var fragmentTransaction = fragmentManager.BeginTransaction();
            _fragment = SupportMapFragment.NewInstance();

            fragmentTransaction.Add(_rootLayout.Id, _fragment);
            fragmentTransaction.Commit();

            _mapReadyCallback = new OnGoogleMapReadyCallback();
            _mapReadyCallback.OnGoogleMapReady += OnMapReady;
            _fragment.GetMapAsync(_mapReadyCallback);
        }

        protected override void DisconnectHandler(FrameLayout platformView)
        {
            if (_fragment is null)
                return;

            DisconnectVirtualView(VirtualView);

            _rootLayout.ViewAttachedToWindow -= OnViewAttachedToWindow;
            _rootLayout.LayoutChange -= OnLayoutChange;

            var fragmentManager = _rootLayout.Context.GetFragmentManager();

            if (fragmentManager?.IsDestroyed == false)
            {
                var fragmentTransaction = fragmentManager.BeginTransaction();
                fragmentTransaction.Remove(_fragment);
                fragmentTransaction.Commit();
            }

            _fragment.Dispose();
            _fragment = null;

            _mapReadyCallback.OnGoogleMapReady -= OnMapReady;
            _mapReadyCallback.Dispose();
            _mapReadyCallback = null;

            if (_map is not null)
            {
                DisconnectGoogleMap(_map);
                _map.Dispose();
                _map = null;
            }
        }

        public static void MapMapTheme(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._map?.UpdateTheme(map.MapTheme, handler.MauiContext.Context);
        }

        public static void MapMapType(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._map?.UpdateType(map.MapType);
        }

        public static void MapIsShowingUser(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._map?.UpdateIsShowingUser(map.IsShowingUser, handler.MauiContext.Context);
        }

        public static void MapShowUserLocationButton(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._map?.UpdateShowUserLocationButton(map.ShowUserLocationButton, handler.MauiContext.Context);
        }

        public static void MapShowCompass(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._map?.UpdateShowCompass(map.ShowCompass);
        }

        public static void MapHasScrollEnabled(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._map?.UpdateHasScrollEnabled(map.HasScrollEnabled);
        }

        public static void MapHasZoomEnabled(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._map?.UpdateHasZoomEnabled(map.HasZoomEnabled);
        }

        public static void MapTrafficEnabled(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?._map?.UpdateTrafficEnabled(map.TrafficEnabled);
        }

        public static void MapSelectedPin(IMapHandler handler, IMap map)
        {
            (handler as MapHandler)?.UpdateSelectedPin();
        }

        public static void MapHeight(IMapHandler handler, IMap map)
        {
        }

        public static void MapMoveToRegion(IMapHandler handler, IMap map, object arg)
        {
            if (arg is MapSpan mapSpan)
                (handler as MapHandler)?.MoveToRegion(mapSpan, true);
        }

        public static void MapViewAttachedToWindow(IMapHandler handler, IMap map, object arg)
        {
        }
        #endregion

        private void OnViewAttachedToWindow(object sender, global::Android.Views.View.ViewAttachedToWindowEventArgs e)
        {
            Invoke(nameof(MapView.ViewAttachedToWindow), null);
        }

        private void OnLayoutChange(object sender, global::Android.Views.View.LayoutChangeEventArgs e)
        {
            if (VirtualView.MoveToLastRegionOnLayoutChange)
            {
                MoveToRegion(VirtualView.LastMoveToRegion, false);
            }

            if (_map is not null)
                UpdateVisibleRegion(_map.CameraPosition.Target);
        }

        private void OnMapReady(object sender, OnGoogleMapReadyEventArgs args)
        {
            if (args.Map is null)
                return;

            _map = args.Map;

            _map.CameraIdle += OnCameraIdle;
            _map.MarkerClick += OnMarkerClick;
            _map.InfoWindowClick += OnInfoWindowClick;
            _map.InfoWindowClose += OnInfoWindowClose;
            _map.InfoWindowLongClick += OnInfoWindowLongClick;
            _map.MapClick += OnMapClick;
            _map.MapLongClick += OnMapLongClick;

            MapMapTheme(this, VirtualView);
            MapMapType(this, VirtualView);
            MapIsShowingUser(this, VirtualView);
            MapShowUserLocationButton(this, VirtualView);
            MapShowCompass(this, VirtualView);
            MapHasScrollEnabled(this, VirtualView);
            MapHasZoomEnabled(this, VirtualView);
            MapTrafficEnabled(this, VirtualView);

            _map.UiSettings.ZoomControlsEnabled = false;
            _map.UiSettings.MapToolbarEnabled = false;

            MoveToRegion(VirtualView.LastMoveToRegion, false);
            OnPinCollectionChanged(VirtualView.Pins, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnMapElementCollectionChanged(VirtualView.MapElements, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            UpdateSelectedPin();
        }

        #region Map
        private void OnMapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            VirtualView.SelectedPin = null;
            VirtualView.SendMapClicked(new Position(e.Point.Latitude, e.Point.Longitude));
        }

        private void OnMapLongClick(object sender, GoogleMap.MapLongClickEventArgs e)
        {
            if (!VirtualView.CanSendMapLongClicked())
                return;

            VirtualView.SelectedPin = null;
            VirtualView.SendMapLongClicked(new Position(e.Point.Latitude, e.Point.Longitude));
        }

        private void MoveToRegion(MapSpan span, bool animate)
        {
            if (_map is null)
                return;

            span = span.ClampLatitude(85, -85);
            var ne = new LatLng(span.Center.Latitude + span.LatitudeDegrees / 2,
                span.Center.Longitude + span.LongitudeDegrees / 2);
            var sw = new LatLng(span.Center.Latitude - span.LatitudeDegrees / 2,
                span.Center.Longitude - span.LongitudeDegrees / 2);
            CameraUpdate update = CameraUpdateFactory.NewLatLngBounds(new LatLngBounds(sw, ne), 0);

            try
            {
                if (animate)
                    _map.AnimateCamera(update);
                else
                    _map.MoveCamera(update);
            }
            catch (IllegalStateException exc)
            {
                System.Diagnostics.Debug.WriteLine($"MapHandler MoveToRegion exception: {exc}");
            }
        }

        private void OnMoveToRegionMessage(IMap s, MapSpan a)
        {
            MoveToRegion(a, true);
        }

        private void UpdateVisibleRegion(LatLng pos)
        {
            if (_map is null)
                return;

            Projection projection = _map.Projection;
            int width = PlatformView.Width;
            int height = PlatformView.Height;
            LatLng ul = projection.FromScreenLocation(new global::Android.Graphics.Point(0, 0));
            LatLng ur = projection.FromScreenLocation(new global::Android.Graphics.Point(width, 0));
            LatLng ll = projection.FromScreenLocation(new global::Android.Graphics.Point(0, height));
            LatLng lr = projection.FromScreenLocation(new global::Android.Graphics.Point(width, height));
            double dlat = Math.Max(Math.Abs(ul.Latitude - lr.Latitude), Math.Abs(ur.Latitude - ll.Latitude));
            double dlong = Math.Max(Math.Abs(ul.Longitude - lr.Longitude), Math.Abs(ur.Longitude - ll.Longitude));
            VirtualView.SetVisibleRegion(new MapSpan(new Position(pos.Latitude, pos.Longitude), dlat, dlong, _map.CameraPosition.Bearing));
        }

        private void UpdateSelectedPin()
        {
            var pin = VirtualView.SelectedPin;

            if (pin is null)
            {
                foreach (var i in _markers.Values)
                    i.marker.HideInfoWindow();
            }
            else if (pin.CanShowInfoWindow && GetMarkerForPin(pin) is Marker m)
            {
                m.ShowInfoWindow();
            }
        }

        private void OnCameraIdle(object sender, EventArgs args)
        {
            UpdateVisibleRegion(_map.CameraPosition.Target);
        }
        #endregion

        #region Pins
        private void AddPins(IList<Pin> pins)
        {
            if (_map is null)
                return;

            foreach (var p in pins)
            {
                if (p.ToHandler(MauiContext) is IMapPinHandler pinHandler)
                {
                    var marker = pinHandler.PlatformView.AddToMap(_map);

                    // associate pin with marker for later lookup in event handlers
                    p.NativeId = marker.Id;

                    if (ReferenceEquals(p, VirtualView.SelectedPin) && p.CanShowInfoWindow)
                        marker.ShowInfoWindow();

                    _markers.Add(marker.Id, (p, marker));
                }
            }
        }

        protected Marker GetMarkerForPin(Pin pin)
            => pin?.NativeId is not null && _markers.TryGetValue((string)pin.NativeId, out var i) ? i.marker : null;

        protected Pin GetPinForMarker(Marker marker)
            => marker?.Id is not null && _markers.TryGetValue(marker.Id, out var i) ? i.pin : null;

        private void OnMarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
            e.Handled = true;

            var marker = e.Marker;
            var pin = GetPinForMarker(marker);

            if (pin is null)
                return;

            if (!ReferenceEquals(pin, VirtualView.SelectedPin))
            {
                VirtualView.SelectedPin = pin;
            }

            if (!pin.CanShowInfoWindow)
                marker.HideInfoWindow();
            else
                marker.ShowInfoWindow();

            VirtualView.SendPinClick(pin);
        }

        private void OnInfoWindowClose(object sender, GoogleMap.InfoWindowCloseEventArgs e)
        {
        }

        private void OnInfoWindowClick(object sender, GoogleMap.InfoWindowClickEventArgs e)
        {
            if (GetPinForMarker(e.Marker) is Pin pin)
                VirtualView.SendInfoWindowClick(pin);
        }

        private void OnInfoWindowLongClick(object sender, GoogleMap.InfoWindowLongClickEventArgs e)
        {
            if (GetPinForMarker(e.Marker) is Pin pin)
                VirtualView.SendInfoWindowLongClick(pin);
        }

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
                    RemovePins(_markers.Values.Select(i => i.pin).ToList());
                    AddPins(VirtualView.Pins);
                    break;
                case NotifyCollectionChangedAction.Move:
                    //do nothing
                    break;
            }
        }

        private void RemovePins(IList<Pin> pins)
        {
            if (_map is null || !_markers.Any())
                return;

            foreach (var p in pins)
            {
                var marker = GetMarkerForPin(p);

                if (marker is null)
                    continue;

                marker.Remove();
                _markers.Remove(marker.Id);
                p.Handler.DisconnectHandler();
            }
        }
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
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddMapElements(e.NewItems.Cast<MapElement>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveMapElements(e.OldItems.Cast<MapElement>());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    RemoveMapElements(e.OldItems.Cast<MapElement>());
                    AddMapElements(e.NewItems.Cast<MapElement>());
                    break;
                case NotifyCollectionChangedAction.Reset:
                    RemoveMapElements(_polylines.Values.Select(i => i.element).ToList());
                    RemoveMapElements(_polygons.Values.Select(i => i.element).ToList());
                    RemoveMapElements(_circles.Values.Select(i => i.element).ToList());

                    AddMapElements(VirtualView.MapElements);
                    break;
            }
        }

        private void AddMapElements(IEnumerable<MapElement> mapElements)
        {
            foreach (var element in mapElements)
            {
                _ = element switch
                {
                    Polyline polyline => AddPolyline(polyline),
                    Polygon polygon => AddPolygon(polygon),
                    Circle circle => AddCircle(circle),
                    _ => throw new NotImplementedException()
                };
            }
        }

        private void RemoveMapElements(IEnumerable<MapElement> mapElements)
        {
            foreach (var element in mapElements)
            {
                _ = element switch
                {
                    Polyline polyline => RemovePolyline(polyline),
                    Polygon polygon => RemovePolygon(polygon),
                    Circle circle => RemoveCircle(circle),
                    _ => throw new NotImplementedException()
                };

                element.Handler.DisconnectHandler();
            }
        }
        #endregion

        #region Polylines
        protected APolyline GetNativePolyline(Polyline polyline)
            => polyline?.MapElementId is not null && _polylines.TryGetValue((string)polyline.MapElementId, out var i) ? i.polyline : null;

        protected Polyline GetVirtualPolyline(APolyline polyline)
            => polyline?.Id is not null && _polylines.TryGetValue(polyline.Id, out var i) ? i.element : null;

        private bool AddPolyline(Polyline polyline)
        {
            if (_map is null) return false;

            if (polyline.ToHandler(MauiContext) is IMapElementHandler elementHandler)
            {
                var nativePolyline = ((MauiMapPolyline)elementHandler.PlatformView).AddToMap(_map);
                polyline.MapElementId = nativePolyline.Id;
                _polylines.Add(nativePolyline.Id, (polyline, nativePolyline));
                return true;
            }

            return false;
        }

        private bool RemovePolyline(Polyline polyline)
        {
            var native = GetNativePolyline(polyline);

            if (native is not null)
            {
                native.Remove();
                return _polylines.Remove(native.Id);
            }

            return false;
        }
        #endregion

        #region Polygons
        protected APolygon GetNativePolygon(Polygon polygon)
            => polygon?.MapElementId is not null && _polygons.TryGetValue((string)polygon.MapElementId, out var i) ? i.polygon : null;

        protected Polygon GetVirtualPolygon(APolygon polygon)
            => polygon?.Id is not null && _polygons.TryGetValue(polygon.Id, out var i) ? i.element : null;

        private bool AddPolygon(Polygon polygon)
        {
            if (_map is null) return false;

            if (polygon.ToHandler(MauiContext) is IMapElementHandler elementHandler)
            {
                var nativePolygon = ((MauiMapPolygon)elementHandler.PlatformView).AddToMap(_map);
                polygon.MapElementId = nativePolygon.Id;
                _polygons.Add(nativePolygon.Id, (polygon, nativePolygon));
                return true;
            }

            return false;
        }

        private bool RemovePolygon(Polygon polygon)
        {
            var native = GetNativePolygon(polygon);

            if (native is not null)
            {
                native.Remove();
                return _polygons.Remove(native.Id);
            }

            return false;
        }
        #endregion

        #region Circles
        protected ACircle GetNativeCircle(Circle circle)
            => circle?.MapElementId is not null && _circles.TryGetValue((string)circle.MapElementId, out var i) ? i.circle : null;

        protected Circle GetVirtualCircle(ACircle circle)
            => circle?.Id is not null && _circles.TryGetValue(circle.Id, out var i) ? i.element : null;

        private bool AddCircle(Circle circle)
        {
            if (_map is null) return false;

            if (circle.ToHandler(MauiContext) is IMapElementHandler elementHandler)
            {
                var nativeCircle = ((MauiMapCircle)elementHandler.PlatformView).AddToMap(_map);
                circle.MapElementId = nativeCircle.Id;
                _circles.Add(nativeCircle.Id, (circle, nativeCircle));
                return true;
            }

            return false;
        }

        private bool RemoveCircle(Circle circle)
        {
            var native = GetNativeCircle(circle);

            if (native is not null)
            {
                native.Remove();
                return _circles.Remove(native.Id);
            }

            return false;
        }
        #endregion

        private void OnVirtualViewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IMap.SelectedPin))
                UpdateSelectedPin();
        }

        private void DisconnectVirtualView(IMap mapModel)
        {
            mapModel.PropertyChanged -= OnVirtualViewPropertyChanged;
            mapModel.Pins.CollectionChanged -= OnPinCollectionChanged;
            mapModel.MapElements.CollectionChanged -= OnMapElementCollectionChanged;

            foreach (var kv in _markers)
            {
                kv.Value.marker.Remove();
                kv.Value.pin.Handler.DisconnectHandler();
            }

            foreach (var kv in _polylines)
            {
                kv.Value.polyline.Remove();
                kv.Value.element.Handler.DisconnectHandler();
            }

            foreach (var kv in _polygons)
            {
                kv.Value.polygon.Remove();
                kv.Value.element.Handler.DisconnectHandler();
            }

            foreach (var kv in _circles)
            {
                kv.Value.circle.Remove();
                kv.Value.element.Handler.DisconnectHandler();
            }

            _markers.Clear();
            _polylines.Clear();
            _polygons.Clear();
        }

        private void DisconnectGoogleMap(GoogleMap mapNative)
        {
            mapNative.MyLocationEnabled = false;
            mapNative.TrafficEnabled = false;

            mapNative.CameraIdle -= OnCameraIdle;
            mapNative.MarkerClick -= OnMarkerClick;
            mapNative.InfoWindowClick -= OnInfoWindowClick;
            mapNative.InfoWindowClose -= OnInfoWindowClose;
            mapNative.InfoWindowLongClick -= OnInfoWindowLongClick;
            mapNative.MapClick -= OnMapClick;
            mapNative.MapLongClick -= OnMapLongClick;
        }
    }
}
