using System.ComponentModel;

namespace BetterMaps.Maui
{
    public class MapElement : Element, IMapElement
    {
        public static readonly BindableProperty StrokeColorProperty = BindableProperty.Create(
            nameof(StrokeColor),
            typeof(Color),
            typeof(MapElement),
            default(Color));

        public static readonly BindableProperty StrokeWidthProperty = BindableProperty.Create(
            nameof(StrokeWidth),
            typeof(float),
            typeof(MapElement),
            5f);

        public Color StrokeColor
        {
            get => (Color)GetValue(StrokeColorProperty);
            set => SetValue(StrokeColorProperty, value);
        }

        public float StrokeWidth
        {
            get => (float)GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public object MapElementId { get; set; }

        #region IStroke
        Paint IStroke.Stroke => StrokeColor?.AsPaint();

        double IStroke.StrokeThickness => StrokeWidth;

        LineCap IStroke.StrokeLineCap => throw new NotImplementedException();

        LineJoin IStroke.StrokeLineJoin => throw new NotImplementedException();

        float[] IStroke.StrokeDashPattern => throw new NotImplementedException();

        float IStroke.StrokeDashOffset => throw new NotImplementedException();

        float IStroke.StrokeMiterLimit => throw new NotImplementedException();
        #endregion
    }
}