using UIKit;

namespace BetterMaps.Maui.iOS
{
    internal static class ImageSourceExtensions
    {
        public static async Task<UIImage> LoadNativeAsync(this ImageSource source, IMauiContext mauiContext, CancellationToken ct)
        {
            var imageResultTask = source switch
            {
                UriImageSource _ => new UriImageSourceService().GetPlatformImageAsync(source, mauiContext),
                FileImageSource _ => new FileImageSourceService().GetPlatformImageAsync(source, mauiContext),
                FontImageSource _ => new FontImageSourceService(mauiContext.Services.GetService<IFontManager>()).GetPlatformImageAsync(source, mauiContext),
                StreamImageSource _ => new StreamImageSourceService().GetPlatformImageAsync(source, mauiContext),
                _ => Task.FromResult(default(IImageSourceServiceResult<UIImage>))
            };

            var imageResult = await imageResultTask.ConfigureAwait(false);
            return imageResult?.Value;
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