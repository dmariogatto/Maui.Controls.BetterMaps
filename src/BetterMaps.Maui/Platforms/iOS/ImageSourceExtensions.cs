using UIKit;

namespace BetterMaps.Maui.iOS
{
    internal static class ImageSourceExtensions
    {
        public static async Task<UIImage> LoadNativeAsync(this ImageSource source, IMauiContext mauiContext, CancellationToken ct)
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
                return imageResult?.Value;
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
