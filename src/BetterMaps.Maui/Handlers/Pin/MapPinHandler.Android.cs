using Android.Gms.Maps.Model;
using Android.Graphics;
using BetterMaps.Maui.Android;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Platform;
using AndroidPaint = Android.Graphics.Paint;
using MauiColor = Microsoft.Maui.Graphics.Color;

namespace BetterMaps.Maui.Handlers
{
    public partial class MapPinHandler : ElementHandler<IMapPin, MauiMapMarker>
    {
        private static readonly TimeSpan ImageCacheTime = TimeSpan.FromMinutes(3);
        private static readonly SemaphoreSlim ImageCacheSemaphore = new SemaphoreSlim(1, 1);

        protected override MauiMapMarker CreatePlatformElement()
            => new MauiMapMarker();

        public static void MapLabel(IMapPinHandler handler, IMapPin pin)
        {
            handler.PlatformView.Title = pin.Label;

            if (handler.PlatformView.IsInfoWindowShown)
                handler.PlatformView.ShowInfoWindow();
        }

        public static void MapAddress(IMapPinHandler handler, IMapPin pin)
        {
            handler.PlatformView.Snippet = pin.Address;

            if (handler.PlatformView.IsInfoWindowShown)
                handler.PlatformView.ShowInfoWindow();
        }

        public static void MapPosition(IMapPinHandler handler, IMapPin pin)
        {
            handler.PlatformView.Position = new LatLng(pin.Position.Latitude, pin.Position.Longitude);
        }

        public static void MapAnchor(IMapPinHandler handler, IMapPin pin)
        {
            handler.PlatformView.Anchor = ((float)pin.Anchor.X, (float)pin.Anchor.Y);
        }

        public static void MapZIndex(IMapPinHandler handler, IMapPin pin)
        {
            handler.PlatformView.ZIndex = pin.ZIndex;
        }

        public static void MapTintColor(IMapPinHandler handler, IMapPin pin)
        {
            UpdateMarkerIcon(handler, pin);
        }

        public static void MapCanShowInfoWindow(IMapPinHandler handler, IMapPin pin)
        {
        }

        public static void MapImageSource(IMapPinHandler handler, IMapPin pin)
        {
            UpdateMarkerIcon(handler, pin);
        }

        protected static void UpdateMarkerIcon(IMapPinHandler handler, IMapPin pin)
        {
            pin.ImageSourceCts?.Cancel();
            pin.ImageSourceCts?.Dispose();
            pin.ImageSourceCts = null;

            var imageTask = GetBitmapFromImageSourceWithTintAsync(handler.MauiContext, pin.ImageSource, pin.TintColor);
            if (imageTask.IsCompletedSuccessfully)
            {
                var image = imageTask.Result;
                handler.PlatformView.Icon = GetBitmapDescriptor(image, pin.TintColor);
            }
            else
            {
                var cts = new CancellationTokenSource();
                var tok = cts.Token;
                pin.ImageSourceCts = cts;

                imageTask.AsTask().ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully && !tok.IsCancellationRequested)
                        ApplyBitmapToMarker(pin, handler.PlatformView, t.Result, tok);
                });
            }
        }

        protected static async ValueTask<Bitmap> GetBitmapFromImageSourceWithTintAsync(IMauiContext mauiContext, ImageSource imgSource, MauiColor tint)
        {
            if (imgSource is null)
                return default;

            var image = default(Bitmap);

            if (tint is not null)
            {
                var imgKey = imgSource.CacheId();
                var cacheKey = !string.IsNullOrEmpty(imgKey)
                    ? $"MCBM_{nameof(GetBitmapFromImageSourceWithTintAsync)}_{imgKey}_{tint.ToHex()}"
                    : string.Empty;

                var cache = mauiContext.Services.GetService<IMapCache>();
                var tintedImage = default(Bitmap);

                if (cache?.TryGetValue(cacheKey, out tintedImage) != true)
                {
                    image = await GetBitmapFromImageSourceAsync(mauiContext, imgSource).ConfigureAwait(false);

                    await ImageCacheSemaphore.WaitAsync().ConfigureAwait(false);

                    try
                    {
                        if (image is not null && cache?.TryGetValue(cacheKey, out tintedImage) != true)
                        {
                            tintedImage = image.Copy(image.GetConfig(), true);
                            var paint = new AndroidPaint();
                            var filter = new PorterDuffColorFilter(tint.ToPlatform(), PorterDuff.Mode.SrcIn);
                            paint.SetColorFilter(filter);
                            var canvas = new Canvas(tintedImage);
                            canvas.DrawBitmap(tintedImage, 0, 0, paint);

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

            return image ?? await GetBitmapFromImageSourceAsync(mauiContext, imgSource).ConfigureAwait(false);
        }

        protected static async ValueTask<Bitmap> GetBitmapFromImageSourceAsync(IMauiContext mauiContext, ImageSource imgSource)
        {
            await ImageCacheSemaphore.WaitAsync().ConfigureAwait(false);

            var imageTask = default(Task<Bitmap>);

            try
            {
                var cache = mauiContext.Services.GetService<IMapCache>();

                var imgKey = imgSource.CacheId();
                var cacheKey = !string.IsNullOrEmpty(imgKey)
                    ? $"MCBM_{nameof(GetBitmapFromImageSourceAsync)}_{imgKey}"
                    : string.Empty;

                var fromCache =
                    !string.IsNullOrEmpty(cacheKey) &&
                    cache?.TryGetValue(cacheKey, out imageTask) == true;

                imageTask ??= imgSource.LoadBitmapFromImageSourceAsync(mauiContext, default);
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
                : default(Bitmap);
        }

        protected static void ApplyBitmapToMarker(IMapPin pin, MauiMapMarker marker, Bitmap image, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
                return;

            var bitmap = GetBitmapDescriptor(image, pin.TintColor);

            void setBitmap()
            {
                if (ct.IsCancellationRequested)
                    return;

                marker.Icon = GetBitmapDescriptor(image, pin.TintColor);
            }

            if (pin is BindableObject bo && bo.Dispatcher.IsDispatchRequired)
                bo.Dispatcher.Dispatch(setBitmap);
            else
                setBitmap();
        }


        protected static BitmapDescriptor GetBitmapDescriptor(Bitmap bitmap, MauiColor color)
        {
            var bitmapDescriptor = default(BitmapDescriptor);

            if (bitmap is not null)
                bitmapDescriptor = BitmapDescriptorFactory.FromBitmap(bitmap);
            else if (color is not null)
                bitmapDescriptor = BitmapDescriptorFactory.DefaultMarker(color.ToAndroidHue());
            else
                bitmapDescriptor = BitmapDescriptorFactory.DefaultMarker();

            return bitmapDescriptor;
        }
    }
}
