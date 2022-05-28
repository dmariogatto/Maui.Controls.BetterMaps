using Android.Gms.Maps.Model;
using Android.Graphics;
using MauiColor = Microsoft.Maui.Graphics.Color;

namespace Maui.Controls.BetterMaps.Android
{
    internal static class MarkerExtensions
    {
        internal static void SetIcon(this Marker marker, Bitmap bitmap, MauiColor color)
            => marker?.SetIcon(GetBitmapDescriptor(bitmap, color));

        internal static void SetIcon(this MarkerOptions options, Bitmap bitmap, MauiColor color)
            => options?.SetIcon(GetBitmapDescriptor(bitmap, color));

        private static BitmapDescriptor GetBitmapDescriptor(Bitmap bitmap, MauiColor color)
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
