using Android.Graphics;
using Android.Graphics.Drawables;

namespace Maui.Controls.BetterMaps.Android
{
    internal static class ImageSourceExtensions
    {
        public static async Task<Bitmap> LoadBitmapFromImageSourceAsync(this ImageSource source, IMauiContext mauiContext, CancellationToken ct)
        {
            var imageResultTask = source switch
            {
                UriImageSource _ => new UriImageSourceService().GetPlatformImageAsync(source, mauiContext),
                FileImageSource _ => new FileImageSourceService().GetPlatformImageAsync(source, mauiContext),
                FontImageSource _ => new FontImageSourceService(mauiContext.Services.GetService<IFontManager>()).GetPlatformImageAsync(source, mauiContext),
                StreamImageSource _ => new StreamImageSourceService().GetPlatformImageAsync(source, mauiContext),
                _ => Task.FromResult(default(IImageSourceServiceResult<Drawable>))
            };

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