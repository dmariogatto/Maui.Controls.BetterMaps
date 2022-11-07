using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Graphics;
using Android.OS;
using Java.Lang;
using Maui.Controls.BetterMaps.Android;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using System.Collections.Specialized;
using System.ComponentModel;
using ACircle = Android.Gms.Maps.Model.Circle;
using AndroidColor = Android.Graphics.Color;
using AndroidPaint = Android.Graphics.Paint;
using APolygon = Android.Gms.Maps.Model.Polygon;
using APolyline = Android.Gms.Maps.Model.Polyline;
using Math = System.Math;
using GCameraUpdate = Android.Gms.Maps.CameraUpdate;

namespace Maui.Controls.BetterMaps
{
    public partial class MapHandler : ViewHandler<IMap, MauiMapView>, IMapHandler
    {
        internal static Bundle Bundle { get; set; }

        private static readonly TimeSpan ImageCacheTime = TimeSpan.FromMinutes(3);
        private static readonly Lazy<Bitmap> BitmapEmpty = new Lazy<Bitmap>(() => Bitmap.CreateBitmap(1, 1, Bitmap.Config.Alpha8));

        private readonly Dictionary<string, (Pin pin, Marker marker)> _markers = new Dictionary<string, (Pin, Marker)>();
        private readonly Dictionary<string, (Polyline element, APolyline polyline)> _polylines = new Dictionary<string, (Polyline, APolyline)>();
        private readonly Dictionary<string, (Polygon element, APolygon polygon)> _polygons = new Dictionary<string, (Polygon, APolygon)>();
        private readonly Dictionary<string, (Circle element, ACircle circle)> _circles = new Dictionary<string, (Circle, ACircle)>();

        private readonly SemaphoreSlim _imgCacheSemaphore = new SemaphoreSlim(1, 1);

        public MapHandler(IPropertyMapper mapper, CommandMapper commandMapper = null)
            : base(mapper, commandMapper)
        {
        }

        #region Overrides

        protected override MauiMapView CreatePlatformView()
        {
            return new MauiMapView(Context);
        }

        protected override void ConnectHandler(MauiMapView platformView)
        {
            platformView.LayoutChange += OnLayoutChange;

            platformView.OnCreate(Bundle);
            platformView.OnResume();

            platformView.OnGoogleMapReady += OnGoogleMapReady;
            platformView.GetMapAsync();

            MessagingCenter.Subscribe<IMap, MapSpan>(this, Map.MoveToRegionMessageName, OnMoveToRegionMessage, VirtualView);

            VirtualView.PropertyChanged += OnVirtualViewPropertyChanged;
            VirtualView.Pins.CollectionChanged += OnPinCollectionChanged;
            VirtualView.MapElements.CollectionChanged += OnMapElementCollectionChanged;

            base.ConnectHandler(platformView);
        }

        protected override void DisconnectHandler(MauiMapView platformView)
        {
            DisconnectVirtualView(VirtualView);

            if (platformView.GoogleMap is not null)
            {
                platformView.LayoutChange -= OnLayoutChange;
                platformView.OnGoogleMapReady -= OnGoogleMapReady;

                DisconnectGoogleMap(platformView.GoogleMap);
            }

            base.DisconnectHandler(platformView);
        }

        public static void MapMapTheme(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateTheme(map.MapTheme, handler.MauiContext.Context);
        }

        public static void MapMapType(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateType(map.MapType);
        }

        public static void MapIsShowingUser(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateIsShowingUser(map.IsShowingUser, handler.MauiContext.Context);
        }

        public static void MapShowUserLocationButton(IMapHandler handler, IMap map)
        {
            handler.PlatformView?.UpdateShowUserLocationButton(map.ShowUserLocationButton, handler.MauiContext.Context);
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
        #endregion

        protected virtual void OnLayoutChange(object sender, global::Android.Views.View.LayoutChangeEventArgs e)
        {
            if (VirtualView.MoveToLastRegionOnLayoutChange)
            {
                MoveToRegion(VirtualView.LastMoveToRegion, true);
            }

            if (PlatformView.GoogleMap is not null)
                UpdateVisibleRegion(PlatformView.GoogleMap.CameraPosition.Target);
        }

        protected virtual void OnMapReady()
        {
            if (PlatformView.GoogleMap is null) return;

            PlatformView.GoogleMap.CameraIdle += OnCameraIdle;
            PlatformView.GoogleMap.MarkerClick += OnMarkerClick;
            PlatformView.GoogleMap.InfoWindowClick += OnInfoWindowClick;
            PlatformView.GoogleMap.InfoWindowClose += OnInfoWindowClose;
            PlatformView.GoogleMap.InfoWindowLongClick += OnInfoWindowLongClick;
            PlatformView.GoogleMap.MapClick += OnMapClick;

            MapMapTheme(this, VirtualView);
            MapMapType(this, VirtualView);
            MapIsShowingUser(this, VirtualView);
            MapShowUserLocationButton(this, VirtualView);
            MapShowCompass(this, VirtualView);
            MapHasScrollEnabled(this, VirtualView);
            MapHasZoomEnabled(this, VirtualView);
            MapTrafficEnabled(this, VirtualView);

            PlatformView.GoogleMap.UiSettings.ZoomControlsEnabled = false;
            PlatformView.GoogleMap.UiSettings.MapToolbarEnabled = false;

            MoveToRegion(VirtualView.LastMoveToRegion, true);
            OnPinCollectionChanged(VirtualView.Pins, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            OnMapElementCollectionChanged(VirtualView.MapElements, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));

            UpdateSelectedPin();
        }

        protected virtual MarkerOptions CreateMarker(Pin pin)
        {
            var opts = new MarkerOptions();

            opts.SetPosition(new LatLng(pin.Position.Latitude, pin.Position.Longitude));
            opts.SetTitle(pin.Label);
            opts.SetSnippet(pin.Address);
            opts.Anchor((float)pin.Anchor.X, (float)pin.Anchor.Y);
            opts.InvokeZIndex(pin.ZIndex);

            pin.ImageSourceCts?.Cancel();
            pin.ImageSourceCts?.Dispose();
            pin.ImageSourceCts = null;

            var imageTask = GetPinImageAsync(pin.ImageSource, pin.TintColor.ToPlatform(Colors.Transparent));

            if (imageTask.IsCompletedSuccessfully)
            {
                var image = imageTask.Result;
                opts.SetIcon(image, pin.TintColor);
            }
            else
            {
                var cts = new CancellationTokenSource();
                var tok = cts.Token;
                pin.ImageSourceCts = cts;

                opts.SetIcon(BitmapDescriptorFactory.FromBitmap(BitmapEmpty.Value));

                imageTask.AsTask().ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && !tok.IsCancellationRequested)
                        ApplyBitmapToMarker(t.Result, pin, tok);
                });
            }

            return opts;
        }

        #region Map
        private void OnMapClick(object sender, GoogleMap.MapClickEventArgs e)
        {
            VirtualView.SelectedPin = null;
            VirtualView.SendMapClicked(new Position(e.Point.Latitude, e.Point.Longitude));
        }

        private void MoveToRegion(MapSpan span, bool animate = true)
        {
            MessagingCenter.Subscribe<Map, MapSpan>(this, "MapMoveToRegion", (sender, arg) =>
            {

                if (PlatformView.GoogleMap is null) return;

                var ne = new LatLng(arg.Center.Latitude + arg.LatitudeDegrees / 2,
                    arg.Center.Longitude + arg.LongitudeDegrees / 2);
                var sw = new LatLng(arg.Center.Latitude - arg.LatitudeDegrees / 2,
                    arg.Center.Longitude - arg.LongitudeDegrees / 2);

                CameraUpdate update = CameraUpdateFactory.NewLatLngBounds(new LatLngBounds(sw, ne), 0);

                try
                {
                    if (animate)
                        PlatformView.GoogleMap.AnimateCamera(update);
                    else
                        PlatformView.GoogleMap.MoveCamera(update);
                }
                catch (IllegalStateException exc)
                {
                    System.Diagnostics.Debug.WriteLine($"MapHandler MoveToRegion exception: {exc}");
                }
            });
        }

        private void OnMoveToRegionMessage(IMap s, MapSpan a)
        {
            MoveToRegion(a, true);
        }

        private void UpdateVisibleRegion(LatLng pos)
        {
            if (PlatformView.GoogleMap is null) return;

            Projection projection = PlatformView.GoogleMap.Projection;
            int width = PlatformView.Width;
            int height = PlatformView.Height;
            LatLng ul = projection.FromScreenLocation(new global::Android.Graphics.Point(0, 0));
            LatLng ur = projection.FromScreenLocation(new global::Android.Graphics.Point(width, 0));
            LatLng ll = projection.FromScreenLocation(new global::Android.Graphics.Point(0, height));
            LatLng lr = projection.FromScreenLocation(new global::Android.Graphics.Point(width, height));
            double dlat = Math.Max(Math.Abs(ul.Latitude - lr.Latitude), Math.Abs(ur.Latitude - ll.Latitude));
            double dlong = Math.Max(Math.Abs(ul.Longitude - lr.Longitude), Math.Abs(ur.Longitude - ll.Longitude));
            VirtualView.SetVisibleRegion(new MapSpan(new Position(pos.Latitude, pos.Longitude), dlat, dlong, PlatformView.GoogleMap.CameraPosition.Bearing));
        }

        private void UpdateSelectedPin()
        {
            var pin = VirtualView.SelectedPin;

            if (pin is null)
            {
                foreach (var i in _markers.Values)
                    i.marker.HideInfoWindow();
            }
            else if (GetMarkerForPin(pin) is Marker m)
            {
                m.ShowInfoWindow();
            }
        }

        private void OnCameraIdle(object sender, EventArgs args)
        {
            UpdateVisibleRegion(PlatformView.GoogleMap.CameraPosition.Target);
        }
        #endregion

        #region Pins
        private void AddPins(IList<Pin> pins)
        {
            if (PlatformView.GoogleMap is null) return;

            foreach (var p in pins)
            {
                var opts = CreateMarker(p);
                var marker = PlatformView.GoogleMap.AddMarker(opts);

                p.PropertyChanged += PinOnPropertyChanged;

                // associate pin with marker for later lookup in event handlers
                p.NativeId = marker.Id;

                if (ReferenceEquals(p, VirtualView.SelectedPin))
                    marker.ShowInfoWindow();

                _markers.Add(marker.Id, (p, marker));
            }
        }

        private void PinOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var pin = (Pin)sender;
            var marker = GetMarkerForPin(pin);

            if (marker is null) return;

            if (e.PropertyName == Pin.LabelProperty.PropertyName)
            {
                marker.Title = pin.Label;
                if (marker.IsInfoWindowShown)
                    marker.ShowInfoWindow();
            }
            else if (e.PropertyName == Pin.AddressProperty.PropertyName)
            {
                marker.Snippet = pin.Address;
                if (marker.IsInfoWindowShown)
                    marker.ShowInfoWindow();
            }
            else if (e.PropertyName == Pin.PositionProperty.PropertyName)
                marker.Position = new LatLng(pin.Position.Latitude, pin.Position.Longitude);
            else if (e.PropertyName == Pin.AnchorProperty.PropertyName)
                marker.SetAnchor((float)pin.Anchor.X, (float)pin.Anchor.Y);
            else if (e.PropertyName == Pin.ZIndexProperty.PropertyName)
                marker.ZIndex = pin.ZIndex;
            else if (e.PropertyName == Pin.ImageSourceProperty.PropertyName ||
                     e.PropertyName == Pin.TintColorProperty.PropertyName)
            {
                pin.ImageSourceCts?.Cancel();
                pin.ImageSourceCts?.Dispose();
                pin.ImageSourceCts = null;

                var imageTask = GetPinImageAsync(pin.ImageSource, pin.TintColor.ToPlatform(Colors.Transparent));
                if (imageTask.IsCompletedSuccessfully)
                {
                    var image = imageTask.Result;
                    marker.SetIcon(image, pin.TintColor);
                }
                else
                {
                    var cts = new CancellationTokenSource();
                    var tok = cts.Token;
                    pin.ImageSourceCts = cts;

                    imageTask.AsTask().ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully && !tok.IsCancellationRequested)
                            ApplyBitmapToMarker(t.Result, pin, tok);
                    });
                }
            }
        }

        private void ApplyBitmapToMarker(Bitmap image, Pin pin, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return;

            void setBitmap()
            {
                if (ct.IsCancellationRequested)
                    return;

                if (GetMarkerForPin(pin) is Marker marker)
                    marker.SetIcon(image, pin.TintColor);
            }

            if (pin.Dispatcher.IsDispatchRequired)
                pin.Dispatcher.Dispatch(setBitmap);
            else
                setBitmap();
        }

        protected virtual async ValueTask<Bitmap> GetPinImageAsync(ImageSource imgSource, AndroidColor tint)
        {
            if (imgSource is null)
                return default;

            var image = default(Bitmap);

            if (tint != Colors.Transparent.ToPlatform())
            {
                var imgKey = imgSource.CacheId();
                var cacheKey = !string.IsNullOrEmpty(imgKey)
                    ? $"MCBM_{nameof(GetPinImageAsync)}_{imgKey}_{tint.ToColor().ToHex()}"
                    : string.Empty;

                var tintedImage = default(Bitmap);
                if (MauiBetterMaps.Cache?.TryGetValue(cacheKey, out tintedImage) != true)
                {
                    image = await GetImageAsync(imgSource).ConfigureAwait(false);

                    await _imgCacheSemaphore.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        if (image is not null && MauiBetterMaps.Cache?.TryGetValue(cacheKey, out tintedImage) != true)
                        {
                            tintedImage = image.Copy(image.GetConfig(), true);
                            var paint = new AndroidPaint();
                            var filter = new PorterDuffColorFilter(tint, PorterDuff.Mode.SrcIn);
                            paint.SetColorFilter(filter);
                            var canvas = new Canvas(tintedImage);
                            canvas.DrawBitmap(tintedImage, 0, 0, paint);

                            if (!string.IsNullOrEmpty(cacheKey))
                                MauiBetterMaps.Cache?.SetSliding(cacheKey, tintedImage, ImageCacheTime);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                    finally
                    {
                        _imgCacheSemaphore.Release();
                    }
                }

                image = tintedImage;
            }

            return image ?? await GetImageAsync(imgSource).ConfigureAwait(false);
        }

        protected virtual async ValueTask<Bitmap> GetImageAsync(ImageSource imgSource)
        {
            await _imgCacheSemaphore.WaitAsync().ConfigureAwait(false);

            var imageTask = default(Task<Bitmap>);

            try
            {
                var imgKey = imgSource.CacheId();
                var cacheKey = !string.IsNullOrEmpty(imgKey)
                    ? $"MCBM_{nameof(GetImageAsync)}_{imgKey}"
                    : string.Empty;

                var fromCache =
                    !string.IsNullOrEmpty(cacheKey) &&
                    MauiBetterMaps.Cache?.TryGetValue(cacheKey, out imageTask) == true;

                imageTask ??= imgSource.LoadBitmapFromImageSourceAsync(MauiContext, default);
                if (!string.IsNullOrEmpty(cacheKey) && !fromCache)
                    MauiBetterMaps.Cache?.SetSliding(cacheKey, imageTask, ImageCacheTime);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                _imgCacheSemaphore.Release();
            }

            return imageTask is not null
                ? await imageTask.ConfigureAwait(false)
                : default(Bitmap);
        }

        protected Marker GetMarkerForPin(Pin pin)
            => pin?.NativeId is not null && _markers.TryGetValue((string)pin.NativeId, out var i) ? i.marker : null;

        protected Pin GetPinForMarker(Marker marker)
            => marker?.Id is not null && _markers.TryGetValue(marker.Id, out var i) ? i.pin : null;

        private void OnMarkerClick(object sender, GoogleMap.MarkerClickEventArgs e)
        {
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
            if (PlatformView.GoogleMap is null || !_markers.Any()) return;

            foreach (var p in pins)
            {
                p.PropertyChanged -= PinOnPropertyChanged;
                var marker = GetMarkerForPin(p);

                if (marker is null)
                    continue;

                marker.Remove();
                _markers.Remove(marker.Id);
            }
        }
        #endregion

        #region MapElements
        private void MapElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (sender)
            {
                case Polyline polyline:
                    PolylineOnPropertyChanged(polyline, e);
                    break;
                case Polygon polygon:
                    PolygonOnPropertyChanged(polygon, e);
                    break;
                case Circle circle:
                    CircleOnPropertyChanged(circle, e);
                    break;
            }
        }

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
                element.PropertyChanged += MapElementPropertyChanged;

                switch (element)
                {
                    case Polyline polyline:
                        AddPolyline(polyline);
                        break;
                    case Polygon polygon:
                        AddPolygon(polygon);
                        break;
                    case Circle circle:
                        AddCircle(circle);
                        break;
                }
            }
        }

        private void RemoveMapElements(IEnumerable<MapElement> mapElements)
        {
            foreach (var element in mapElements)
            {
                element.PropertyChanged -= MapElementPropertyChanged;

                switch (element)
                {
                    case Polyline polyline:
                        RemovePolyline(polyline);
                        break;
                    case Polygon polygon:
                        RemovePolygon(polygon);
                        break;
                    case Circle circle:
                        RemoveCircle(circle);
                        break;
                }

                element.MapElementId = null;
            }
        }
        #endregion

        #region Polylines
        protected virtual PolylineOptions CreatePolylineOptions(Polyline polyline)
        {
            var opts = new PolylineOptions();

            opts.InvokeColor(polyline.StrokeColor.ToPlatform(Colors.Black));
            opts.InvokeWidth(polyline.StrokeWidth);

            foreach (var position in polyline.Geopath)
            {
                opts.Points.Add(new LatLng(position.Latitude, position.Longitude));
            }

            return opts;
        }

        protected APolyline GetNativePolyline(Polyline polyline)
            => polyline?.MapElementId is not null && _polylines.TryGetValue((string)polyline.MapElementId, out var i) ? i.polyline : null;

        protected Polyline GetFormsPolyline(APolyline polyline)
            => polyline?.Id is not null && _polylines.TryGetValue(polyline.Id, out var i) ? i.element : null;

        private void PolylineOnPropertyChanged(Polyline formsPolyline, PropertyChangedEventArgs e)
        {
            var nativePolyline = GetNativePolyline(formsPolyline);

            if (nativePolyline is null) return;

            if (e.PropertyName == MapElement.StrokeColorProperty.PropertyName)
                nativePolyline.Color = formsPolyline.StrokeColor.ToPlatform(Colors.Black);
            else if (e.PropertyName == MapElement.StrokeWidthProperty.PropertyName)
                nativePolyline.Width = formsPolyline.StrokeWidth;
            else if (e.PropertyName == nameof(Polyline.Geopath))
                nativePolyline.Points = formsPolyline.Geopath.Select(position => new LatLng(position.Latitude, position.Longitude)).ToList();
        }

        private void AddPolyline(Polyline polyline)
        {
            if (PlatformView.GoogleMap is null) return;

            var options = CreatePolylineOptions(polyline);
            var nativePolyline = PlatformView.GoogleMap.AddPolyline(options);

            polyline.MapElementId = nativePolyline.Id;

            _polylines.Add(nativePolyline.Id, (polyline, nativePolyline));
        }

        private void RemovePolyline(Polyline polyline)
        {
            var native = GetNativePolyline(polyline);

            if (native is not null)
            {
                native.Remove();
                _polylines.Remove(native.Id);
            }
        }
        #endregion

        #region Polygons
        protected virtual PolygonOptions CreatePolygonOptions(Polygon polygon)
        {
            var opts = new PolygonOptions();

            opts.InvokeStrokeColor(polygon.StrokeColor.ToPlatform(Colors.Black));
            opts.InvokeStrokeWidth(polygon.StrokeWidth);

            if (polygon.FillColor.IsNotDefault())
                opts.InvokeFillColor(polygon.FillColor.ToPlatform());

            // Will throw an exception when added to the map if Points is empty
            if (polygon.Geopath.Count == 0)
            {
                opts.Points.Add(new LatLng(0, 0));
            }
            else
            {
                foreach (var position in polygon.Geopath)
                {
                    opts.Points.Add(new LatLng(position.Latitude, position.Longitude));
                }
            }

            return opts;
        }

        protected APolygon GetNativePolygon(Polygon polygon)
            => polygon?.MapElementId is not null && _polygons.TryGetValue((string)polygon.MapElementId, out var i) ? i.polygon : null;

        protected Polygon GetFormsPolygon(APolygon polygon)
            => polygon?.Id is not null && _polygons.TryGetValue(polygon.Id, out var i) ? i.element : null;

        private void PolygonOnPropertyChanged(Polygon polygon, PropertyChangedEventArgs e)
        {
            var nativePolygon = GetNativePolygon(polygon);

            if (nativePolygon is null) return;

            if (e.PropertyName == MapElement.StrokeColorProperty.PropertyName)
                nativePolygon.StrokeColor = polygon.StrokeColor.ToPlatform(Colors.Black);
            else if (e.PropertyName == MapElement.StrokeWidthProperty.PropertyName)
                nativePolygon.StrokeWidth = polygon.StrokeWidth;
            else if (e.PropertyName == Polygon.FillColorProperty.PropertyName)
                nativePolygon.FillColor = polygon.FillColor.ToPlatform(Colors.Black);
            else if (e.PropertyName == nameof(polygon.Geopath))
                nativePolygon.Points = polygon.Geopath.Select(p => new LatLng(p.Latitude, p.Longitude)).ToList();
        }

        private void AddPolygon(Polygon polygon)
        {
            if (PlatformView.GoogleMap is null) return;

            var options = CreatePolygonOptions(polygon);
            var nativePolygon = PlatformView.GoogleMap.AddPolygon(options);

            polygon.MapElementId = nativePolygon.Id;

            _polygons.Add(nativePolygon.Id, (polygon, nativePolygon));
        }

        private void RemovePolygon(Polygon polygon)
        {
            var native = GetNativePolygon(polygon);

            if (native is not null)
            {
                native.Remove();
                _polygons.Remove(native.Id);
            }
        }
        #endregion

        #region Circles
        protected virtual CircleOptions CreateCircleOptions(Circle circle)
        {
            var opts = new CircleOptions()
                .InvokeCenter(new LatLng(circle.Center.Latitude, circle.Center.Longitude))
                .InvokeRadius(circle.Radius.Meters)
                .InvokeStrokeWidth(circle.StrokeWidth);

            if (circle.StrokeColor.IsNotDefault())
                opts.InvokeStrokeColor(circle.StrokeColor.ToPlatform());

            if (circle.FillColor.IsNotDefault())
                opts.InvokeFillColor(circle.FillColor.ToPlatform());

            return opts;
        }

        protected ACircle GetNativeCircle(Circle circle)
            => circle?.MapElementId is not null && _circles.TryGetValue((string)circle.MapElementId, out var i) ? i.circle : null;

        protected Circle GetFormsCircle(ACircle circle)
            => circle?.Id is not null && _circles.TryGetValue(circle.Id, out var i) ? i.element : null;

        private void CircleOnPropertyChanged(Circle formsCircle, PropertyChangedEventArgs e)
        {
            var nativeCircle = GetNativeCircle(formsCircle);

            if (nativeCircle is null) return;

            if (e.PropertyName == Circle.FillColorProperty.PropertyName)
                nativeCircle.FillColor = formsCircle.FillColor.ToPlatform(Colors.Black);
            else if (e.PropertyName == Circle.CenterProperty.PropertyName)
                nativeCircle.Center = new LatLng(formsCircle.Center.Latitude, formsCircle.Center.Longitude);
            else if (e.PropertyName == Circle.RadiusProperty.PropertyName)
                nativeCircle.Radius = formsCircle.Radius.Meters;
            else if (e.PropertyName == MapElement.StrokeColorProperty.PropertyName)
                nativeCircle.StrokeColor = formsCircle.StrokeColor.ToPlatform(Colors.Black);
            else if (e.PropertyName == MapElement.StrokeWidthProperty.PropertyName)
                nativeCircle.StrokeWidth = formsCircle.StrokeWidth;
        }

        private void AddCircle(Circle circle)
        {
            if (PlatformView.GoogleMap is null) return;

            var options = CreateCircleOptions(circle);
            var nativeCircle = PlatformView.GoogleMap.AddCircle(options);

            circle.MapElementId = nativeCircle.Id;

            _circles.Add(nativeCircle.Id, (circle, nativeCircle));
        }

        private void RemoveCircle(Circle circle)
        {
            var native = GetNativeCircle(circle);

            if (native is not null)
            {
                native.Remove();
                _circles.Remove(native.Id);
            }
        }
        #endregion

        private void OnVirtualViewPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(IMap.SelectedPin))
                UpdateSelectedPin();
        }

        private void DisconnectVirtualView(IMap mapModel)
        {
            MessagingCenter.Unsubscribe<Map, MapSpan>(this, Map.MoveToRegionMessageName);

            mapModel.PropertyChanged -= OnVirtualViewPropertyChanged;
            mapModel.Pins.CollectionChanged -= OnPinCollectionChanged;
            mapModel.MapElements.CollectionChanged -= OnMapElementCollectionChanged;

            foreach (var kv in _markers)
            {
                kv.Value.pin.PropertyChanged -= PinOnPropertyChanged;
                kv.Value.pin.NativeId = null;
                kv.Value.marker.Remove();
            }

            foreach (var kv in _polylines)
            {
                kv.Value.element.PropertyChanged -= MapElementPropertyChanged;
                kv.Value.element.MapElementId = null;
                kv.Value.polyline.Remove();
            }

            foreach (var kv in _polygons)
            {
                kv.Value.element.PropertyChanged -= MapElementPropertyChanged;
                kv.Value.element.MapElementId = null;
                kv.Value.polygon.Remove();
            }

            foreach (var kv in _circles)
            {
                kv.Value.element.PropertyChanged -= MapElementPropertyChanged;
                kv.Value.element.MapElementId = null;
                kv.Value.circle.Remove();
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
        }

        private void OnGoogleMapReady(object sender, OnGoogleMapReadyEventArgs e)
        {
            OnMapReady();
        }
    }
}
