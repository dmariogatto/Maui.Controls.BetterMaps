using Android.Graphics;
using Android.Graphics.Drawables;

namespace BetterMaps.Maui.Android
{
    internal static class ImageSourceExtensions
    {
        public static async Task<Bitmap> LoadBitmapFromImageSourceAsync(this ImageSource source, IMauiContext mauiContext, CancellationToken ct)
        {
            var provider = mauiContext?.Services.GetService<IImageSourceServiceProvider>();

            var imageResultTask = source switch
            {
                UriImageSource _ => provider?.GetImageSourceService<UriImageSource>()?.GetPlatformImageAsync(source, mauiContext),
                FileImageSource _ => provider?.GetImageSourceService<FileImageSource>()?.GetPlatformImageAsync(source, mauiContext),
                FontImageSource _ => provider?.GetImageSourceService<FontImageSource>()?.GetPlatformImageAsync(source, mauiContext),
                StreamImageSource _ => provider?.GetImageSourceService<StreamImageSource>()?.GetPlatformImageAsync(source, mauiContext),
                _ => null
            };

            if (imageResultTask is not null)
            {
                var imageResult = await imageResultTask.ConfigureAwait(false);

                if (imageResult?.Value is Drawable drawable)
                {
                    var canvas = new Canvas();
                    var bitmap = Bitmap.CreateBitmap(drawable.IntrinsicWidth, drawable.IntrinsicHeight, Bitmap.Config.Argb8888);
                    canvas.SetBitmap(bitmap);

                    drawable.SetBounds(0, 0, drawable.IntrinsicWidth, drawable.IntrinsicHeight);
                    drawable.Draw(canvas);

                    return bitmap;
                }
            }

            return null;
        }

        public static string CacheId(this ImageSource source) => source switch
        {
            UriImageSource uriSource => uriSource.Uri.OriginalString,
            FileImageSource fileSource => fileSource.File,
            FontImageSource fontSource => $"{fontSource.FontFamily}_{fontSource.Glyph}",
            _ => null
        };
    }
}
