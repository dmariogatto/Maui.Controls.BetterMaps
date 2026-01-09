using BetterMaps.Maui.iOS;
using CoreGraphics;
using CoreLocation;
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

            var pin = mauiMapView.VirtualViewForAnnotation(annotation) as Pin ?? throw new NullReferenceException("Pin cannot be null");
            var handler = pin.Handler as MapPinHandler ?? throw new NullReferenceException("PinHandler cannot be null");
            handler._mapViewRef = new WeakReference<MKMapView>(mapView);

            pin.CancelImageCts();

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
                        pin.SetImageCts(cts);

                        imageTask.ContinueWith(t =>
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
#pragma warning disable CA1416 // Validate platform compatibility
                view.ZPriority = pin.ZIndex;
#pragma warning restore CA1416 // Validate platform compatibility
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
                pin.CancelImageCts();

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
                            pin.SetImageCts(cts);

                            imageTask.ContinueWith(t =>
                            {
                                if (t.IsCompletedSuccessfully && !tok.IsCancellationRequested)
                                    ApplyUIImageToView(pin, view, t.Result, tok);
                            });
                        }
                        break;
                }
            }
        }

        protected static async Task<UIImage> GetUIImageFromImageSourceWithTintAsync(IMauiContext mauiContext, ImageSource imgSource, UIColor tint)
        {
            if (imgSource is null)
                return null;
            if (tint is null)
                return await GetUIImageFromImageSourceAsync(mauiContext, imgSource).ConfigureAwait(false);

            var cache = mauiContext.Services.GetService<IMapCache>();

            var imgKey = imgSource.CacheId();
            var cacheKey = !string.IsNullOrEmpty(imgKey)
                ? $"MCBM_{nameof(GetUIImageFromImageSourceWithTintAsync)}_{imgKey}_{tint.ToColor().ToHex()}"
                : string.Empty;

            if (cache?.TryGetValue(cacheKey, out UIImage tintedImage) == true)
                return tintedImage;

            var image = await GetUIImageFromImageSourceAsync(mauiContext, imgSource).ConfigureAwait(false);

            await ImageCacheSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (cache?.TryGetValue(cacheKey, out tintedImage) == true)
                    return tintedImage;

                if (image is not null)
                {
                    tintedImage = await GetTintedImageAsync(image, tint).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(cacheKey))
                        cache?.SetSliding(cacheKey, tintedImage, ImageCacheTime);
                    return tintedImage;
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

            return null;
        }

        protected static async Task<UIImage> GetUIImageFromImageSourceAsync(IMauiContext mauiContext, ImageSource imgSource)
        {
            var cache = mauiContext.Services.GetService<IMapCache>();

            var imgKey = imgSource.CacheId();
            var cacheKey = !string.IsNullOrEmpty(imgKey)
                ? $"MCBM_{nameof(GetUIImageFromImageSourceAsync)}_{imgKey}"
                : string.Empty;

            if (cache?.TryGetValue(cacheKey, out UIImage image) == true)
                return image;

            await ImageCacheSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (cache?.TryGetValue(cacheKey, out image) == true)
                    return image;
                image = await imgSource.LoadNativeAsync(mauiContext, CancellationToken.None).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(cacheKey))
                    cache?.SetSliding(cacheKey, image, ImageCacheTime);
                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                ImageCacheSemaphore.Release();
            }

            return null;
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

        private static Task<UIImage> GetTintedImageAsync(UIImage image, UIColor tint)
        {
            var tcs = new TaskCompletionSource<UIImage>();

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    var renderer = new UIGraphicsImageRenderer(image.Size,
                        new UIGraphicsImageRendererFormat()
                        {
                            Opaque = false,
                            Scale = image.CurrentScale,
                        });

                    var resultImage = renderer.CreateImage(imageContext =>
                    {
                        tint.SetFill();
                        imageContext.CGContext.TranslateCTM(0, image.Size.Height);
                        imageContext.CGContext.ScaleCTM(1, -1);
                        var rect = new CGRect(0, 0, image.Size.Width, image.Size.Height);
                        imageContext.CGContext.ClipToMask(rect, image.CGImage);
                        imageContext.CGContext.FillRect(rect);
                    });

                    tcs.SetResult(resultImage);
                }
                catch (Exception ex)
                {
                    tcs.SetException(ex);
                }
            });

            return tcs.Task;
        }
    }
}
