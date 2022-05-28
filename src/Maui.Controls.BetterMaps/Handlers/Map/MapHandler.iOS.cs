using CoreGraphics;
using CoreLocation;
using Foundation;
using MapKit;
using Maui.Controls.BetterMaps.iOS;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using System.Collections.Specialized;
using System.ComponentModel;
using UIKit;
using RectangleF = CoreGraphics.CGRect;

namespace Maui.Controls.BetterMaps
{
    public partial class MapHandler : ViewHandler<IMap, MauiMapView>, IMapHandler
    {
        protected readonly TimeSpan ImageCacheTime = TimeSpan.FromMinutes(3);

        private static readonly Lazy<UIImage> UIImageEmpty = new Lazy<UIImage>(() => new UIImage());

        private readonly Dictionary<IMKAnnotation, Pin> _pinLookup = new Dictionary<IMKAnnotation, Pin>();
        private readonly Dictionary<IMKOverlay, MapElement> _elementLookup = new Dictionary<IMKOverlay, MapElement>();

        private readonly SemaphoreSlim _imgCacheSemaphore = new SemaphoreSlim(1, 1);

        private bool _shouldUpdateRegion;
        private bool _init = true;

        private UITapGestureRecognizer _mapClickedGestureRecognizer;

        public MapHandler(IPropertyMapper mapper, CommandMapper commandMapper = null)
            : base(mapper, commandMapper)
        {
        }

        #region Overrides

        protected override MauiMapView CreatePlatformView()
        {
            return new MauiMapView(RectangleF.Empty);
        }

        protected override void ConnectHandler(MauiMapView platformView)
        {
            platformView.GetViewForAnnotation = GetViewForAnnotation;
            platformView.OverlayRenderer = GetViewForOverlay;
            platformView.OnLayoutSubviews += OnLayoutSubviews;
            platformView.DidSelectAnnotationView += MkMapViewOnAnnotationViewSelected;
            platformView.DidDeselectAnnotationView += MkMapViewOnAnnotationViewDeselected;
            platformView.RegionChanged += MkMapViewOnRegionChanged;
            platformView.AddGestureRecognizer(_mapClickedGestureRecognizer = new UITapGestureRecognizer(OnMapClicked));

            MessagingCenter.Subscribe<IMap, MapSpan>(this, Map.MoveToRegionMessageName, (s, a) => MoveToRegion(a), VirtualView);

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
            handler.PlatformView?.UpdateShowUserLocationButton(map.ShowUserLocationButton, handler.PlatformView.UserTrackingButton);
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
        protected virtual IMKAnnotation CreateAnnotation(Pin pin)
            => new MauiPointAnnotation(pin);

        protected virtual MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            var mapPin = default(MKAnnotationView);

            // https://bugzilla.xamarin.com/show_bug.cgi?id=26416
            var userLocationAnnotation = Runtime.GetNSObject(annotation.Handle) as MKUserLocation;
            if (userLocationAnnotation is not null)
                return null;

            const string defaultPinAnnotationId = nameof(defaultPinAnnotationId);
            const string customImgAnnotationId = nameof(customImgAnnotationId);

            var fAnnotation = (MauiPointAnnotation)annotation;
            var pin = fAnnotation.Pin;

            pin.ImageSourceCts?.Cancel();
            pin.ImageSourceCts?.Dispose();
            pin.ImageSourceCts = null;

            var imageTask = GetPinImageAsync(fAnnotation.ImageSource, fAnnotation.TintColor);
            if (!imageTask.IsCompletedSuccessfully || imageTask.Result is not null)
            {
                var cts = new CancellationTokenSource();
                var tok = cts.Token;
                pin.ImageSourceCts = cts;

                mapPin = mapView.DequeueReusableAnnotation(customImgAnnotationId);

                if (mapPin is null)
                {
                    mapPin = new MKAnnotationView(annotation, customImgAnnotationId);
                }

                mapPin.Annotation = annotation;
                mapPin.Layer.AnchorPoint = fAnnotation.Anchor;

                if (imageTask.IsCompletedSuccessfully)
                {
                    var image = imageTask.Result;
                    mapPin.Image = image;
                }
                else
                {
                    mapPin.Image = UIImageEmpty.Value;

                    imageTask.AsTask().ContinueWith(t =>
                    {
                        if (t.IsCompletedSuccessfully && !tok.IsCancellationRequested)
                            ApplyUIImageToView(t.Result, mapPin, tok);
                    });
                }

                if (MauiBetterMaps.Ios14OrNewer)
                    mapPin.ZPriority = fAnnotation.ZIndex;
                mapPin.CanShowCallout = pin.CanShowInfoWindow;
            }
            else
            {
                mapPin = mapView.DequeueReusableAnnotation(defaultPinAnnotationId);

                if (mapPin is null)
                {
                    mapPin = new MKPinAnnotationView(annotation, defaultPinAnnotationId);
                }

                mapPin.Annotation = annotation;
                ((MKPinAnnotationView)mapPin).PinTintColor =
                    !fAnnotation.TintColor.IsEqual(Colors.Transparent.ToPlatform())
                    ? fAnnotation.TintColor
                    : null;

                if (MauiBetterMaps.Ios14OrNewer)
                    mapPin.ZPriority = fAnnotation.ZIndex;
                mapPin.CanShowCallout = pin.CanShowInfoWindow;
            }

            return mapPin;
        }

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
                        RecenterMap();
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

        private void RecenterMap()
        {
            // workaround (long press not registered until map movement)
            // https://developer.apple.com/forums/thread/126473
            var map = PlatformView;
            map.SetCenterCoordinate(map.CenterCoordinate, false);
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
            if (VirtualView is null) return;

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
                p.PropertyChanged -= PinOnPropertyChanged;

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
            var selectedAnnotation = default(IMKAnnotation);

            var annotations = pins.Select(p =>
            {
                p.PropertyChanged += PinOnPropertyChanged;
                var annotation = CreateAnnotation(p);
                p.NativeId = annotation;

                if (ReferenceEquals(p, VirtualView.SelectedPin))
                    selectedAnnotation = annotation;

                _pinLookup.Add(annotation, p);

                return annotation;
            }).ToArray();

            PlatformView.AddAnnotations(annotations);

            if (selectedAnnotation is not null)
                PlatformView.SelectAnnotation(selectedAnnotation, true);
        }

        private void PinOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is Pin pin &&
                pin.NativeId is MauiPointAnnotation annotation &&
                ReferenceEquals(pin, annotation.Pin))
            {
                if (e.PropertyName == Pin.LabelProperty.PropertyName)
                {
                    annotation.SetValueForKey(new NSString(pin.Label), new NSString(nameof(annotation.Title)));
                }
                else if (e.PropertyName == Pin.AddressProperty.PropertyName)
                {
                    annotation.SetValueForKey(new NSString(pin.Address), new NSString(nameof(annotation.Subtitle)));
                }
                else if (e.PropertyName == Pin.PositionProperty.PropertyName)
                {
                    var coord = new CLLocationCoordinate2D(pin.Position.Latitude, pin.Position.Longitude);
                    ((IMKAnnotation)annotation).SetCoordinate(coord);
                }
                else if (e.PropertyName == Pin.AnchorProperty.PropertyName)
                {
                    if (PlatformView.ViewForAnnotation(annotation) is MKAnnotationView view)
                        view.Layer.AnchorPoint = annotation.Anchor;
                }
                else if (e.PropertyName == Pin.ZIndexProperty.PropertyName)
                {
                    if (MauiBetterMaps.Ios14OrNewer && PlatformView.ViewForAnnotation(annotation) is MKAnnotationView view)
                        view.SetValueForKey(new NSNumber((float)annotation.ZIndex), new NSString(nameof(view.ZPriority)));
                }
                else if (e.PropertyName == Pin.CanShowInfoWindowProperty.PropertyName)
                {
                    if (PlatformView.ViewForAnnotation(annotation) is MKAnnotationView view)
                        view.CanShowCallout = pin.CanShowInfoWindow;
                }
                else if (e.PropertyName == Pin.ImageSourceProperty.PropertyName ||
                         e.PropertyName == Pin.TintColorProperty.PropertyName)
                {
                    pin.ImageSourceCts?.Cancel();
                    pin.ImageSourceCts?.Dispose();
                    pin.ImageSourceCts = null;

                    switch (PlatformView.ViewForAnnotation(annotation))
                    {
                        case MKPinAnnotationView pinView:
                            var tintColor = !annotation.TintColor.IsEqual(Colors.Transparent.ToPlatform()) ? annotation.TintColor : null;
                            pinView.SetValueForKey(tintColor, new NSString(nameof(pinView.PinTintColor)));
                            break;
                        case MKAnnotationView view:
                            var cts = new CancellationTokenSource();
                            var tok = cts.Token;
                            pin.ImageSourceCts = cts;

                            var imageTask = GetPinImageAsync(annotation.ImageSource, annotation.TintColor);
                            if (imageTask.IsCompletedSuccessfully)
                            {
                                var image = imageTask.Result;
                                view.SetValueForKey(image, new NSString(nameof(view.Image)));
                            }
                            else
                            {
                                imageTask.AsTask().ContinueWith(t =>
                                {
                                    if (t.IsCompletedSuccessfully && !tok.IsCancellationRequested)
                                        ApplyUIImageToView(t.Result, view, tok);
                                });
                            }
                            break;
                    }
                }
            }
        }

        private void ApplyUIImageToView(UIImage image, MKAnnotationView view, CancellationToken ct)
        {
            if (ct.IsCancellationRequested || image is null)
                return;

            void setImage()
            {
                if (ct.IsCancellationRequested)
                    return;
                view.SetValueForKey(image, new NSString(nameof(view.Image)));
            }

            if (VirtualView is BindableObject bo && bo.Dispatcher.IsDispatchRequired)
                bo.Dispatcher.Dispatch(setImage);
            else
                setImage();
        }

        protected virtual async ValueTask<UIImage> GetPinImageAsync(ImageSource imgSource, UIColor tint)
        {
            if (imgSource is null)
                return default;

            var image = default(UIImage);

            if (!tint.IsEqual(Colors.Transparent.ToPlatform()))
            {
                var imgKey = imgSource.CacheId();
                var cacheKey = !string.IsNullOrEmpty(imgKey)
                    ? $"MCBM_{nameof(GetPinImageAsync)}_{imgKey}_{tint.ToColor().ToHex()}"
                    : string.Empty;

                var tintedImage = default(UIImage);
                if (MauiBetterMaps.Cache?.TryGetValue(cacheKey, out tintedImage) != true)
                {
                    image = await GetImageAsync(imgSource).ConfigureAwait(false);

                    await _imgCacheSemaphore.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        if (image is not null && MauiBetterMaps.Cache?.TryGetValue(cacheKey, out tintedImage) != true)
                        {
                            UIGraphics.BeginImageContextWithOptions(image.Size, false, image.CurrentScale);
                            var context = UIGraphics.GetCurrentContext();
                            tint.SetFill();
                            context.TranslateCTM(0, image.Size.Height);
                            context.ScaleCTM(1, -1);
                            var rect = new CGRect(0, 0, image.Size.Width, image.Size.Height);
                            context.ClipToMask(new CGRect(0, 0, image.Size.Width, image.Size.Height), image.CGImage);
                            context.FillRect(rect);
                            tintedImage = UIGraphics.GetImageFromCurrentImageContext();
                            UIGraphics.EndImageContext();

                            if (!string.IsNullOrEmpty(cacheKey))
                                MauiBetterMaps.Cache?.SetSliding(cacheKey, tintedImage, ImageCacheTime);
                        }
                    }
                    catch (Exception ex)
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

        protected virtual async ValueTask<UIImage> GetImageAsync(ImageSource imgSource)
        {
            await _imgCacheSemaphore.WaitAsync().ConfigureAwait(false);

            var imageTask = default(Task<UIImage>);

            try
            {
                var imgKey = imgSource.CacheId();
                var cacheKey = !string.IsNullOrEmpty(imgKey)
                    ? $"MCBM_{nameof(GetImageAsync)}_{imgKey}"
                    : string.Empty;

                var fromCache =
                    !string.IsNullOrEmpty(cacheKey) &&
                    MauiBetterMaps.Cache?.TryGetValue(cacheKey, out imageTask) == true;

                imageTask ??= imgSource.LoadNativeAsync(MauiContext, default);
                if (!string.IsNullOrEmpty(cacheKey) && !fromCache)
                    MauiBetterMaps.Cache?.SetSliding(cacheKey, imageTask, ImageCacheTime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                _imgCacheSemaphore.Release();
            }

            return imageTask is not null
                ? await imageTask.ConfigureAwait(false)
                : default(UIImage);
        }

        protected Pin GetPinForAnnotation(IMKAnnotation annotation)
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
            var overlays = mapElements.Select(e =>
            {
                e.PropertyChanged += MapElementPropertyChanged;

                IMKOverlay overlay = e switch
                {
                    Polyline polyline => MKPolyline.FromCoordinates(polyline.Geopath
                            .Select(position => new CLLocationCoordinate2D(position.Latitude, position.Longitude))
                            .ToArray()),
                    Polygon polygon => MKPolygon.FromCoordinates(polygon.Geopath
                            .Select(position => new CLLocationCoordinate2D(position.Latitude, position.Longitude))
                            .ToArray()),
                    Circle circle => MKCircle.Circle(
                            new CLLocationCoordinate2D(circle.Center.Latitude, circle.Center.Longitude),
                            circle.Radius.Meters),
                    _ => throw new NotSupportedException("Element not supported")

                };

                e.MapElementId = overlay;
                _elementLookup.Add(overlay, e);

                return overlay;
            }).ToArray();

            PlatformView.AddOverlays(overlays);
        }

        private void RemoveMapElements(IEnumerable<MapElement> mapElements)
        {
            var overlays = mapElements.Select(e =>
            {
                e.PropertyChanged -= MapElementPropertyChanged;

                var overlay = (IMKOverlay)e.MapElementId;
                _elementLookup.Remove(overlay);
                e.MapElementId = null;

                return overlay;
            }).ToArray();

            PlatformView.RemoveOverlays(overlays);
        }

        private void MapElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var element = (MapElement)sender;

            RemoveMapElements(new[] { element });
            AddMapElements(new[] { element });
        }

        protected virtual MKOverlayRenderer GetViewForOverlay(MKMapView mapview, IMKOverlay overlay)
            => overlay switch
            {
                MKPolyline polyline => GetViewForPolyline(polyline),
                MKPolygon polygon => GetViewForPolygon(polygon),
                MKCircle circle => GetViewForCircle(circle),
                _ => null
            };

        protected virtual MKPolylineRenderer GetViewForPolyline(MKPolyline mkPolyline)
            => mkPolyline is not null && _elementLookup.TryGetValue(mkPolyline, out var e) && e is Polyline pl
                ? new MKPolylineRenderer(mkPolyline)
                {
                    StrokeColor = pl.StrokeColor.ToPlatform(Colors.Black),
                    LineWidth = pl.StrokeWidth
                }
                : null;

        protected virtual MKPolygonRenderer GetViewForPolygon(MKPolygon mkPolygon)
            => mkPolygon is not null && _elementLookup.TryGetValue(mkPolygon, out var e) && e is Polygon pg
                ? new MKPolygonRenderer(mkPolygon)
                {
                    StrokeColor = pg.StrokeColor.ToPlatform(Colors.Black),
                    FillColor = pg.FillColor?.ToPlatform(),
                    LineWidth = pg.StrokeWidth
                }
                : null;

        protected virtual MKCircleRenderer GetViewForCircle(MKCircle mkCircle)
            => mkCircle is not null && _elementLookup.TryGetValue(mkCircle, out var e) && e is Circle c
                ? new MKCircleRenderer(mkCircle)
                {
                    StrokeColor = c.StrokeColor.ToPlatform(Colors.Black),
                    FillColor = c.FillColor?.ToPlatform(),
                    LineWidth = c.StrokeWidth
                }
                : null;
        #endregion

        private void CleanUpMapModelElements(IMap mapModel, MKMapView mapNative)
        {
            MessagingCenter.Unsubscribe<IMap, MapSpan>(this, Map.MoveToRegionMessageName);
            mapModel.PropertyChanged -= OnVirtualViewPropertyChanged;
            mapModel.Pins.CollectionChanged -= OnPinCollectionChanged;
            mapModel.MapElements.CollectionChanged -= OnMapElementCollectionChanged;

            foreach (var kv in _pinLookup)
            {
                kv.Value.PropertyChanged -= PinOnPropertyChanged;
                kv.Value.NativeId = null;
            }

            foreach (var kv in _elementLookup)
            {
                kv.Value.PropertyChanged -= MapElementPropertyChanged;
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
