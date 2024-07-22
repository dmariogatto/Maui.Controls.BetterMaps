using BetterMaps.Maui.iOS;
using CoreGraphics;
using CoreLocation;
using Foundation;
using MapKit;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using UIKit;

namespace BetterMaps.Maui.Handlers
{
    public partial class MapPinHandler : ElementHandler<IMapPin, IMKAnnotation>
    {
        private static readonly TimeSpan ImageCacheTime = TimeSpan.FromMinutes(3);
        private static readonly SemaphoreSlim ImageCacheSemaphore = new SemaphoreSlim(1, 1);

        private WeakReference<MKMapView> _mapViewRef;

        protected override IMKAnnotation CreatePlatformElement()
            => new MKPointAnnotation();

        public static MKAnnotationView GetViewForAnnotation(MKMapView mapView, IMKAnnotation annotation)
        {
            if (mapView?.Superview is not MauiMapView mauiMapView)
                return null;

            var view = default(MKAnnotationView);

            // https://bugzilla.xamarin.com/show_bug.cgi?id=26416
            var userLocationAnnotation = Runtime.GetNSObject(annotation.Handle) as MKUserLocation;
            if (userLocationAnnotation is not null)
                return null;

            const string defaultPinAnnotationId = nameof(defaultPinAnnotationId);
            const string customImgAnnotationId = nameof(customImgAnnotationId);

            var mauiPointAnnotation = (MKPointAnnotation)annotation;

            if (mauiMapView.VirtualViewForAnnotation(annotation) is not Pin pin)
                throw new NullReferenceException("Pin cannot be null");
            var handler = pin.Handler as MapPinHandler ?? throw new NullReferenceException("PinHandler cannot be null");
            handler._mapViewRef = new WeakReference<MKMapView>(mapView);

            pin.ImageSourceCts?.Cancel();
            pin.ImageSourceCts?.Dispose();
            pin.ImageSourceCts = null;

            var imageTask = GetUIImageFromImageSourceWithTintAsync(handler.MauiContext, pin.ImageSource, pin.TintColor?.ToPlatform());

            if (!imageTask.IsCompletedSuccessfully || imageTask.Result is not null)
            {
                view = mapView.DequeueReusableAnnotation(customImgAnnotationId);
                view ??= new MKAnnotationView(annotation, customImgAnnotationId);

                if (imageTask.IsCompletedSuccessfully)
                {
                    var image = imageTask.Result;
                    view.Image = image;
                }
                else
                {
                    view.Image = null;

                    if (!imageTask.IsFaulted && !imageTask.IsCanceled)
                    {
                        var cts = new CancellationTokenSource();
                        var tok = cts.Token;
                        pin.ImageSourceCts = cts;

                        imageTask.AsTask().ContinueWith(t =>
                        {
                            if (t.IsCompletedSuccessfully && !tok.IsCancellationRequested)
                                ApplyUIImageToView(pin, view, t.Result, tok);
                        });
                    }
                }
            }
            else
            {
                view = mapView.DequeueReusableAnnotation(defaultPinAnnotationId);
                view ??= OperatingSystem.IsIOSVersionAtLeast(11)
                    ? new MKMarkerAnnotationView(annotation, defaultPinAnnotationId) { RightCalloutAccessoryView = new UIView() }
                    : new MKPinAnnotationView(annotation, defaultPinAnnotationId);

                var tintColor = pin.TintColor?.ToPlatform();
#pragma warning disable CA1422 // Validate platform compatibility
                _ = view switch
                {
                    MKMarkerAnnotationView markerAnnotationView => markerAnnotationView.MarkerTintColor = tintColor,
                    MKPinAnnotationView pinAnnotationView => pinAnnotationView.PinTintColor = tintColor,
                    _ => throw new NotImplementedException()
                };
#pragma warning restore CA1422 // Validate platform compatibility
            }

            view.Annotation = annotation;
            view.Layer.AnchorPoint = new CGPoint(pin.Anchor.X, pin.Anchor.Y);
            if (OperatingSystem.IsIOSVersionAtLeast(14))
                view.ZPriority = pin.ZIndex;
            view.CanShowCallout = pin.CanShowInfoWindow;

            return view;
        }

        protected override void DisconnectHandler(IMKAnnotation platformView)
        {
            _mapViewRef = null;
            base.DisconnectHandler(platformView);
        }

        public static void MapLabel(IMapPinHandler handler, IMapPin pin)
        {
            if (handler.PlatformView is MKPointAnnotation annotation)
                annotation.Title = pin.Label ?? string.Empty;
        }

        public static void MapAddress(IMapPinHandler handler, IMapPin pin)
        {
            if (handler.PlatformView is MKPointAnnotation annotation)
                annotation.Subtitle = pin.Address ?? string.Empty;
        }

        public static void MapPosition(IMapPinHandler handler, IMapPin pin)
        {
            if (handler.PlatformView is MKPointAnnotation annotation)
            {
                var coord = new CLLocationCoordinate2D(pin.Position.Latitude, pin.Position.Longitude);
                ((IMKAnnotation)annotation).SetCoordinate(coord);
            }
        }

        public static void MapAnchor(IMapPinHandler handler, IMapPin pin)
        {
            if (handler is not MapPinHandler pinHandler)
                return;
            if (handler.PlatformView is not MKPointAnnotation annotation)
                return;

            if (pinHandler._mapViewRef?.TryGetTarget(out var mapView) == true && mapView.ViewForAnnotation(annotation) is MKAnnotationView view)
                view.Layer.AnchorPoint = new CGPoint(pin.Anchor.X, pin.Anchor.Y);
        }

        public static void MapZIndex(IMapPinHandler handler, IMapPin pin)
        {
            if (!OperatingSystem.IsIOSVersionAtLeast(14))
                return;
            if (handler is not MapPinHandler pinHandler)
                return;
            if (handler.PlatformView is not MKPointAnnotation annotation)
                return;

            if (pinHandler._mapViewRef?.TryGetTarget(out var mapView) == true && mapView.ViewForAnnotation(annotation) is MKAnnotationView view)
#pragma warning disable CA1416 // Validate platform compatibility
                view.ZPriority = pin.ZIndex;
#pragma warning restore CA1416 // Validate platform compatibility
        }

        public static void MapCanShowInfoWindow(IMapPinHandler handler, IMapPin pin)
        {
            if (handler is not MapPinHandler pinHandler)
                return;
            if (handler.PlatformView is not MKPointAnnotation annotation)
                return;

            if (pinHandler._mapViewRef?.TryGetTarget(out var mapView) == true && mapView.ViewForAnnotation(annotation) is MKAnnotationView view)
                view.CanShowCallout = pin.CanShowInfoWindow;
        }

        public static void MapTintColor(IMapPinHandler handler, IMapPin pin)
        {
            UpdateAnnotationIcon(handler, pin);
        }

        public static void MapImageSource(IMapPinHandler handler, IMapPin pin)
        {
            UpdateAnnotationIcon(handler, pin);
        }

        protected static void UpdateAnnotationIcon(IMapPinHandler handler, IMapPin pin)
        {
            if (handler is not MapPinHandler pinHandler)
                return;
            if (handler.PlatformView is not MKPointAnnotation annotation)
                return;

            if (pinHandler._mapViewRef?.TryGetTarget(out var mapView) == true && mapView.ViewForAnnotation(annotation) is MKAnnotationView view)
            {
                pin.ImageSourceCts?.Cancel();
                pin.ImageSourceCts?.Dispose();
                pin.ImageSourceCts = null;

                switch (view)
                {
                    case MKMarkerAnnotationView markerAnnotationView:
                        markerAnnotationView.MarkerTintColor = pin.TintColor?.ToPlatform();
                        break;
                    case MKPinAnnotationView pinAnnotationView:
#pragma warning disable CA1422 // Validate platform compatibility
                        pinAnnotationView.PinTintColor = pin.TintColor?.ToPlatform();
#pragma warning restore CA1422 // Validate platform compatibility
                        break;
                    default:
                        var imageTask = GetUIImageFromImageSourceWithTintAsync(handler.MauiContext, pin.ImageSource, pin.TintColor?.ToPlatform());
                        if (imageTask.IsCompletedSuccessfully)
                        {
                            var image = imageTask.Result;
                            view.Image = image;
                        }
                        else if (!imageTask.IsFaulted && !imageTask.IsCanceled)
                        {
                            var cts = new CancellationTokenSource();
                            var tok = cts.Token;
                            pin.ImageSourceCts = cts;

                            imageTask.AsTask().ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully && !tok.IsCancellationRequested)
                                    ApplyUIImageToView(pin, view, t.Result, tok);
                            });
                        }
                        break;
                }
            }
        }

        protected static async ValueTask<UIImage> GetUIImageFromImageSourceWithTintAsync(IMauiContext mauiContext, ImageSource imgSource, UIColor tint)
        {
            if (imgSource is null)
                return default;

            var image = default(UIImage);

            if (tint is not null)
            {
                var imgKey = imgSource.CacheId();
                var cacheKey = !string.IsNullOrEmpty(imgKey)
                    ? $"MCBM_{nameof(GetUIImageFromImageSourceWithTintAsync)}_{imgKey}_{tint.ToColor().ToHex()}"
                    : string.Empty;

                var cache = mauiContext.Services.GetService<IMapCache>();
                var tintedImage = default(UIImage);

                if (cache?.TryGetValue(cacheKey, out tintedImage) != true)
                {
                    image = await GetUIImageFromImageSourceAsync(mauiContext, imgSource).ConfigureAwait(false);

                    await ImageCacheSemaphore.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        if (image is not null && cache?.TryGetValue(cacheKey, out tintedImage) != true)
                        {
                            var renderer = new UIGraphicsImageRenderer(image.Size, new UIGraphicsImageRendererFormat()
                            {
                                Opaque = false,
                                Scale = image.CurrentScale,
                            });

                            tintedImage = renderer.CreateImage(imageContext =>
                            {
                                tint.SetFill();
                                imageContext.CGContext.TranslateCTM(0, image.Size.Height);
                                imageContext.CGContext.ScaleCTM(1, -1);
                                var rect = new CGRect(0, 0, image.Size.Width, image.Size.Height);
                                imageContext.CGContext.ClipToMask(rect, image.CGImage);
                                imageContext.CGContext.FillRect(rect);
                            });

                            if (!string.IsNullOrEmpty(cacheKey))
                                cache?.SetSliding(cacheKey, tintedImage, ImageCacheTime);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                    }
                    finally
                    {
                        ImageCacheSemaphore.Release();
                    }
                }

                image = tintedImage;
            }

            return image ?? await GetUIImageFromImageSourceAsync(mauiContext, imgSource).ConfigureAwait(false);
        }

        protected static async ValueTask<UIImage> GetUIImageFromImageSourceAsync(IMauiContext mauiContext, ImageSource imgSource)
        {
            await ImageCacheSemaphore.WaitAsync().ConfigureAwait(false);

            var imageTask = default(Task<UIImage>);

            try
            {
                var cache = mauiContext.Services.GetService<IMapCache>();

                var imgKey = imgSource.CacheId();
                var cacheKey = !string.IsNullOrEmpty(imgKey)
                    ? $"MCBM_{nameof(GetUIImageFromImageSourceAsync)}_{imgKey}"
                    : string.Empty;

                var fromCache =
                    !string.IsNullOrEmpty(cacheKey) &&
                    cache?.TryGetValue(cacheKey, out imageTask) == true;

                imageTask ??= imgSource.LoadNativeAsync(mauiContext, default);
                if (!string.IsNullOrEmpty(cacheKey) && !fromCache)
                    cache?.SetSliding(cacheKey, imageTask, ImageCacheTime);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                ImageCacheSemaphore.Release();
            }

            return imageTask is not null
                ? await imageTask.ConfigureAwait(false)
                : default(UIImage);
        }

        protected static void ApplyUIImageToView(IMapPin pin, MKAnnotationView view, UIImage image, CancellationToken ct)
        {
            if (ct.IsCancellationRequested || image is null)
                return;

            void setImage()
            {
                if (ct.IsCancellationRequested)
                    return;
                view.Image = image;
            }

            if (pin is BindableObject bo && bo.Dispatcher.IsDispatchRequired)
                bo.Dispatcher.Dispatch(setImage);
            else
                setImage();
        }
    }
}
